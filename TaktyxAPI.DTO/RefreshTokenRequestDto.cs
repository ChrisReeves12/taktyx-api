using System.ComponentModel.DataAnnotations;

namespace TaktyxAPI.DTO;

public class RefreshTokenRequestDto
{
    [Required]
    public string ExpiredToken { get; set; }
    
    [Required]
    public string RefreshToken { get; set; }
}