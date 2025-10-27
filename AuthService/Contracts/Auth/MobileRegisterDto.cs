using AuthService.Contracts.Auth.Both;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Contracts.Auth;

public record MobileRegisterDto : PhoneNumberDto
{
    [RegularExpression(@"^(?!\s*$)(?=.*[A-Za-z])\S{8,}$", ErrorMessage = "Password format wouldn't meet requirement")]
    public required string Password { get; set; }

    public string? PhoneBrand { get; set; }
}
