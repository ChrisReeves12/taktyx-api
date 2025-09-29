using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
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
    private readonly IUserRepository _userRepository;
    private readonly IMailService _mailService;
    private readonly int _passwordResetExpiryMin;

    public AuthController(IAuthTokenService authTokenService, IPasswordService passwordService,
     IConfiguration configuration, TaktyxDbContext dbContext, IMailService mailService, IUserRepository userRepository)
    {
        _authTokenService = authTokenService;
        _passwordService = passwordService;
        _dbContext = dbContext;
        _mailService = mailService;
        _userRepository = userRepository;
        _passwordResetExpiryMin = configuration.GetValue<int>("PasswordResetExpiryMin");
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

    [HttpPost("password-reset/otp")]
    public async Task<ActionResult> SendPasswordResetRequest([FromBody] SendPasswordResetRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = request.Email.ToLower().Trim();
        var user = await _userRepository.GetByEmailAsync(email);

        // For security, do not reveal existence. Still do best-effort mail for existing user.
        if (user == null)
        {
            // Return 200 OK to avoid user enumeration
            return Ok();
        }

        // Generate 6-digit OTP via mail service
        var otp = _mailService.GenerateOtp(6);

        user.PasswordResetCode = otp;
        user.PasswordResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(_passwordResetExpiryMin);
        user.PasswordResetAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        await _mailService.SendPasswordResetOtpAsync(user.Email, otp, _passwordResetExpiryMin);

        return Ok();
    }

    [HttpPost("password-reset")]
    public async Task<ActionResult> ResetUserPassword([FromBody] ResetUserPasswordDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!request.Password.Equals(request.ConfirmPassword))
        {
            return BadRequest("Password and confirmation password do not match");
        }

        var email = request.Email.ToLower().Trim();
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(user.PasswordResetCode) || user.PasswordResetCodeExpiresAt == null)
        {
            return Unauthorized();
        }

        if (DateTime.UtcNow > user.PasswordResetCodeExpiresAt)
        {
            user.PasswordResetCode = null;
            user.PasswordResetCodeExpiresAt = null;
            user.PasswordResetAttempts = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return Unauthorized("Password reset code expired. Please request a new code.");
        }

        var attempts = user.PasswordResetAttempts ?? 0;
        if (!string.Equals(user.PasswordResetCode, request.PasswordResetCode))
        {
            attempts += 1;
            if (attempts >= 3)
            {
                user.PasswordResetCode = null;
                user.PasswordResetCodeExpiresAt = null;
                user.PasswordResetAttempts = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                return Unauthorized("Too many invalid attempts. Please request a new code.");
            }

            user.PasswordResetAttempts = attempts;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            var remaining = 3 - attempts;
            return Unauthorized($"Invalid code. {remaining} attempt(s) remaining.");
        }

        // Valid code
        user.Password = _passwordService.HashPassword(request.Password);
        user.PasswordResetCode = null;
        user.PasswordResetCodeExpiresAt = null;
        user.PasswordResetAttempts = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return Ok();
    }
}