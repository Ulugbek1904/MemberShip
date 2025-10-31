using Common.Models.Base;
using Domain.Models.Org;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Auth;

[Table("user_to_company", Schema = "auth")]
[Index(nameof(CompanyId))]
[Index(nameof(UserId))]
public class UserToCompany : AuditableModelBase<long>
{
    [ForeignKey(nameof(Company))]
    public long CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    [ForeignKey(nameof(User))]
    public long UserId { get; set; }
    public VendorUser User { get; set; } = default!;
}
