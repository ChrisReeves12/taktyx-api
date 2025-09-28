using Microsoft.Extensions.Configuration;
using TaktyxAPI.DTO;
using TaktyxAPI.Service.Interfaces;

namespace TaktyxAPI.Service;

public class GoogleRouteLocationService : IRouteLocationService
{
    private readonly string _googleAPIKey;
    private readonly HttpClient _httpClient;

    public GoogleRouteLocationService(IConfiguration configuration, HttpClient httpClient)
    {
        _googleAPIKey = configuration.GetValue<string>("GoogleAPIKey") ?? string.Empty;
        _httpClient = httpClient;
    }
    
    public Task<List<RouteMatrixResponseDto>> GetRouteDistanceMatrix(double originLat, double originLng, LatLngDto[] destinations)
    {
        throw new NotImplementedException();
    }
}