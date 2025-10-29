using Common.Common.Extensions;
using Common.Common.Helpers;
using Common.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Auth;

public class User : SoftDeletableAndAuditableModelBase<long>
{
    public string Name { get; set; }
    public string Username { get; set; } = default!;
    [EmailAddress]
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? PhoneNumber { get; set; }

    [ForeignKey(nameof(UserRole))]
    public long? UserRoleId { get; set; }
    public UserRole? UserRole { get; set; }

    public long? CompanyId { get; set; }


    public ICollection<UserDevice> UserDevices { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserToCompany> UserToCompanies { get; set; } = [];
    public bool IsValidPassword(string password)
    {
        return !(password.IsNullOrEmpty() || (EnvironmentHelper.IsProduction
                      ? PasswordHelper.Encrypt(password)
                      : password) != PasswordHash);
    }
}
