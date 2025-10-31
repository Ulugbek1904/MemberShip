using Common.Common.Extensions;
using Common.Common.Helpers;
using Common.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Auth;

public class VendorUser : SoftDeletableAndAuditableModelBase<long>
{
    [EmailAddress]
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
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
