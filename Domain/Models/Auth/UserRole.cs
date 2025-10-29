using Common.Common.Models;
using Common.Models.Base;
using Domain.Models.Org;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Auth;

[Table("user_role", Schema = "auth")]
public class UserRole : AuditableModelBase<long>
{
    public MultiLanguageField Name { get; set; } = default!;

    [ForeignKey(nameof(Company))]
    public long CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public List<Permission> Permissions { get; set; } = default!;
    public bool IsSystemDefined { get; set; }
    public bool IsDirectorRole { get; set; }

    public List<PermissionToUserRole>? PermissionToUserRoles { get; set; }
}
