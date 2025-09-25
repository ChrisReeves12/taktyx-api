namespace TaktyxAPI.DTO;

public class RefreshTokenDto
{
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}