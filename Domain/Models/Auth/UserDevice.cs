using Common.Models.Base;
using Domain.Models.Org;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Auth;

[Table("user_devices", Schema ="auth")]
public class UserDevice : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))]
    public long UserId { get; set; }
    public virtual VendorUser User { get; set; } = default!;

    [ForeignKey(nameof(Company))]
    public long? CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    public string Token { get; set; } = default!;
    public DateTime ExpiredDate { get; set; }
    public string DeviceName { get; set; } = default!;
    public string? PhoneBrand { get; set; }
}
