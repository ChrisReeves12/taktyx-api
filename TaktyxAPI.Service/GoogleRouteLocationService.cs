using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text;
using System.Text.Json;
using TaktyxAPI.DTO;
using TaktyxAPI.Service.Interfaces;

namespace TaktyxAPI.Service;

// Request/Response DTOs for Google Routes API
public class GoogleRouteMatrixRequest
{
    public List<RouteMatrixOrigin> origins { get; set; } = new();
    public List<RouteMatrixDestination> destinations { get; set; } = new();
    public string travelMode { get; set; } = "DRIVE";
    public string routingPreference { get; set; } = "TRAFFIC_AWARE";
}

public class RouteMatrixOrigin
{
    public RouteMatrixWaypoint waypoint { get; set; } = new();
}

public class RouteMatrixDestination
{
    public RouteMatrixWaypoint waypoint { get; set; } = new();
}

public class RouteMatrixWaypoint
{
    public RouteMatrixLocation location { get; set; } = new();
}

public class RouteMatrixLocation
{
    public LatLng latLng { get; set; } = new();
}

public class LatLng
{
    public double latitude { get; set; }
    public double longitude { get; set; }
}

public class GoogleRouteMatrixResponse
{
    public int originIndex { get; set; }
    public int destinationIndex { get; set; }
    public RouteStatus status { get; set; } = new();
    public int distanceMeters { get; set; }
    public string duration { get; set; } = string.Empty;
    public string condition { get; set; } = string.Empty;
}

public class RouteStatus
{
    // Empty object in successful responses
}

public class GoogleRouteLocationService : IRouteLocationService
{
    private readonly string _googleAPIKey;
    private readonly HttpClient _httpClient;

    public GoogleRouteLocationService(IConfiguration configuration, HttpClient httpClient)
    {
        _googleAPIKey = configuration.GetValue<string>("GoogleAPIKey") ?? string.Empty;
        _httpClient = httpClient;
    }

    public async Task<List<RouteMatrixResponseDto>> GetRouteDistanceMatrix(double originLat, double originLng, LatLngDto[] destinations)
    {
        // Build the request body
        var request = new GoogleRouteMatrixRequest
        {
            origins = new List<RouteMatrixOrigin>
            {
                new RouteMatrixOrigin
                {
                    waypoint = new RouteMatrixWaypoint
                    {
                        location = new RouteMatrixLocation
                        {
                            latLng = new LatLng
                            {
                                latitude = originLat,
                                longitude = originLng
                            }
                        }
                    }
                }
            },
            destinations = destinations.Select(dest => new RouteMatrixDestination
            {
                waypoint = new RouteMatrixWaypoint
                {
                    location = new RouteMatrixLocation
                    {
                        latLng = new LatLng
                        {
                            latitude = dest.Latitude,
                            longitude = dest.Longitude
                        }
                    }
                }
            }).ToList(),
            travelMode = "DRIVE",
            routingPreference = "TRAFFIC_AWARE"
        };

        // Serialize request to JSON
        var jsonContent = JsonSerializer.Serialize(request);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Create the request
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://routes.googleapis.com/distanceMatrix/v2:computeRouteMatrix")
        {
            Content = httpContent
        };

        // Add headers
        requestMessage.Headers.Add("X-Goog-Api-Key", _googleAPIKey);
        requestMessage.Headers.Add("X-Goog-FieldMask", "originIndex,destinationIndex,duration,distanceMeters,status,condition");

        // Make the request
        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Google Routes API request failed with status code: {response.StatusCode}");
        }

        // Read and deserialize the response
        var responseJson = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<List<GoogleRouteMatrixResponse>>(responseJson);

        if (apiResponse == null)
        {
            throw new JsonException("Failed to deserialize Google Routes API response");
        }

        // Map API response to RouteMatrixResponseDto
        var result = new List<RouteMatrixResponseDto>();
        foreach (var item in apiResponse)
        {
            if (item.condition != "ROUTE_EXISTS")
            {
                continue;
            }
            
            // Get the destination coordinates using the destinationIndex
            var destination = destinations[item.destinationIndex];
            var durationSeconds = ParseDuration(item.duration);

            result.Add(new RouteMatrixResponseDto
            {
                Latitude = destination.Latitude,
                Longitude = destination.Longitude,
                DistanceMeters = item.distanceMeters,
                Duration = durationSeconds,
                Name = $"Route to destination {item.destinationIndex}"
            });
        }

        return result;
    }

    private static double ParseDuration(string duration)
    {
        if (string.IsNullOrEmpty(duration))
            return 0;

        if (duration.EndsWith('s'))
        {
            var secondsPart = duration.TrimEnd('s');
            if (double.TryParse(secondsPart, out var seconds))
            {
                return seconds;
            }
        }

        return 0;
    }
}