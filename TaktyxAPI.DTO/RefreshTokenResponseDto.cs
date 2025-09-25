namespace TaktyxAPI.DTO;

public class RefreshTokenResponseDto
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime TokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
}