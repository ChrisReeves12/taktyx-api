namespace TaktyxAPI.DTO;

public class JwtSettingsDto
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 24;
    public int RefreshTokenExpirationHours { get; set; } = 720;
}