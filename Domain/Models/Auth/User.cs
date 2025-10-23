using Common.Models.Base;

namespace Domain.Models.Auth;

public class User : SoftDeletableAndAuditableModelBase<long>
{
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

}
