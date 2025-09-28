using TaktyxAPI.DTO;

namespace TaktyxAPI.Service.Interfaces;

public interface IRouteLocationService
{
    public Task<List<RouteMatrixResponseDto>> GetRouteDistanceMatrix(double originLat, 
        double originLng, LatLngDto[] destinations);
}