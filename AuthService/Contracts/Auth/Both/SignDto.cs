using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Contracts.Auth.Both;

public record SignDto
{
    public required string Password { get; set; }
    [EmailAddress]
    public required string Email { get; set; }

    [DefaultValue(false)]
    public bool IsMobile { get; set; }
}
