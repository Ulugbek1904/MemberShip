using System.ComponentModel.DataAnnotations;

namespace AuthService.Contracts.Auth.Both;

public record PhoneNumberDto
{
    [RegularExpression(@"^\+*998\d{9}$", ErrorMessage = "Phone number format wouldn't meet UZB phone number format")]
    public required string PhoneNumber { get; set; }
}
