using System.ComponentModel.DataAnnotations;

namespace TaktyxAPI.DTO;

public class AuthDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email.")]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; }
}

public class AuthResultDto
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public int UserId { get; set; }
    public DateTime Expiration { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
}