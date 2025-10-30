using Common.Models.Base;
using Domain.Models.Org;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Auth;

[Table("refresh_tokens", Schema = "auth")]
[Index(nameof(UserId))]
public class RefreshToken : AuditableModelBase<long>
{
    [ForeignKey("User")]
    public long UserId { get; set; }
    public virtual VendorUser User { get; set; } = default!;

    [ForeignKey(nameof(Company))]
    public long? CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    public string TokenHash { get; set; } = default!;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; }
}
