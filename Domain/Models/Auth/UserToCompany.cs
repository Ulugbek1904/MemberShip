using Common.Models.Base;
using Domain.Enum.Auth;
using Domain.Models.Org;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Auth;

[Table("user_to_company", Schema = "auth")]
[Index(nameof(UserType))]
[Index(nameof(CompanyId))]
[Index(nameof(UserId))]
public class UserToCompany : AuditableModelBase<long>
{
    public EnumUserType UserType { get; set; }

    [ForeignKey(nameof(Company))]
    public long CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    [ForeignKey(nameof(User))]
    public long UserId { get; set; }
    public User User { get; set; } = default!;
}
