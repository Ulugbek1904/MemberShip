using AuthService.Contracts.Auth.Both;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Contracts.Auth;

public record MobileRegisterDto 
{
    [EmailAddress]
    public required string Email { get; set; }
    [RegularExpression(@"^(?!\s*$)(?=.*[A-Za-z])\S{8,}$", ErrorMessage = "Password format wouldn't meet requirement")]
    public required string Password { get; set; }
    [MaxLength(6, ErrorMessage = "Otp code must contain 6 numbers")]
    [MinLength(6, ErrorMessage = "Otp code must contain 6 numbers")]
    public required string Otp {  get; set; }
    public string? PhoneBrand { get; set; }
}