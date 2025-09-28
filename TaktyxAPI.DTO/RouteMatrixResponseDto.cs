namespace TaktyxAPI.DTO;

public class RouteMatrixResponseDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
    public double Duration { get; set; }
    public string Name { get; set; } = string.Empty;
}