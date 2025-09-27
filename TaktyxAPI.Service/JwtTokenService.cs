using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaktyxAPI.DTO;
using TaktyxAPI.Service.Interfaces;

namespace TaktyxAPI.Service;

public class JwtTokenService : IAuthTokenService
{
    private readonly JwtSettingsDto _jwtSettings;

    public JwtTokenService(JwtSettingsDto jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public TokenDto GenerateToken(int userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email.ToLower()),
            new Claim(ClaimTypes.Role, role),
            new Claim("userId", userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new TokenDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };
    }

    public TokenValidationResultDto? ValidateToken(string token, bool validateLifetime = true)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var userIdString = principal.FindFirst("userId")?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (userIdString == null || email == null || !int.TryParse(userIdString, out var userId))
            {
                return null;
            }

            return new TokenValidationResultDto { Email = email, UserId = userId };
        }
        catch
        {
            return null;
        }
    }

    public RefreshTokenDto GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return new RefreshTokenDto
        {
            RefreshToken = Convert.ToBase64String(randomNumber),
            ExpiresAt = DateTime.Now.AddHours(_jwtSettings.RefreshTokenExpirationHours)
        };
    }
}