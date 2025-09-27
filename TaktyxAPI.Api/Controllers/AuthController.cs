using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaktyxAPI.Data.Data;
using TaktyxAPI.DTO;
using TaktyxAPI.Service.Interfaces;

namespace TaktyxAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthTokenService _authTokenService;
    private readonly IPasswordService _passwordService;
    private readonly TaktyxDbContext _dbContext;

    public AuthController(IAuthTokenService authTokenService, IPasswordService passwordService, TaktyxDbContext dbContext)
    {
        _authTokenService = authTokenService;
        _passwordService = passwordService;
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<AuthResultDto>> Authenticate(AuthDto authDto)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.Equals(authDto.Email.ToLower().Trim()));
        if (user is null)
        {
            return NotFound();
        }

        if (!_passwordService.VerifyPassword(authDto.Password, user.Password))
        {
            return Unauthorized();
        }

        // Generate tokens
        var tokenResponse = _authTokenService.GenerateToken(user.Id, user.Email, user.Role);
        var refreshTokenResponse = _authTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshTokenResponse.RefreshToken;
        user.RefreshTokenExpiresAt = refreshTokenResponse.ExpiresAt;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new AuthResultDto
        {
            Token = tokenResponse.Token,
            RefreshToken = refreshTokenResponse.RefreshToken,
            UserId = user.Id,
            Expiration = tokenResponse.ExpiresAt,
            RefreshTokenExpiration = refreshTokenResponse.ExpiresAt
        });
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult> RefreshToken(RefreshTokenRequestDto request)
    {
        var validationResult = _authTokenService.ValidateToken(request.ExpiredToken, false);
        if (validationResult == null)
        {
            return Unauthorized();
        }

        // Generate new tokens
        var user = await _dbContext.Users.FindAsync(validationResult.UserId);
        if (user == null)
        {
            return NotFound();
        }

        if (user.RefreshToken == null || user.RefreshTokenExpiresAt == null ||
            !user.RefreshToken.Equals(request.RefreshToken) || DateTime.UtcNow > user.RefreshTokenExpiresAt)
        {
            return Unauthorized();
        }

        var tokenResponse = _authTokenService.GenerateToken(validationResult.UserId, validationResult.Email, user.Role);
        var refreshTokenResponse = _authTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshTokenResponse.RefreshToken;
        user.RefreshTokenExpiresAt = refreshTokenResponse.ExpiresAt;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new RefreshTokenResponseDto
        {
            RefreshToken = refreshTokenResponse.RefreshToken,
            Token = tokenResponse.Token,
            RefreshTokenExpiresAt = refreshTokenResponse.ExpiresAt,
            TokenExpiresAt = tokenResponse.ExpiresAt
        });
    }
}