using TaktyxAPI.DTO;

namespace TaktyxAPI.Service.Interfaces;

public interface IAuthTokenService
{
    public TokenDto GenerateToken(int userId, string email);
    public TokenValidationResultDto? ValidateToken(string token, bool validateLifetime = true);
    public RefreshTokenDto GenerateRefreshToken();
}