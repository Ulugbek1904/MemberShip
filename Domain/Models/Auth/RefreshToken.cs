using Common.Models.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Auth;

[Table("refresh_tokens", Schema = "auth")]
[Index(nameof(UserId))]
public class RefreshToken : AuditableModelBase<long>
{
    [ForeignKey("User")]
    public long UserId { get; set; }
    public virtual User User { get; set; } = default!;

    public string TokenHash { get; set; } = default!;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; }
}
