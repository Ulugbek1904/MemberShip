using System.ComponentModel.DataAnnotations;

namespace AuthService.Contracts.Auth.Both;

public record SignDto
{
    [EmailAddress]
    public required string Email { get; set; }
    public required string Password { get; set; }
}
