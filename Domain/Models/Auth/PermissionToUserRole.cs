using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Auth;

[Table("permission_to_user_role", Schema = "auth")]
public class PermissionToUserRole
{
    [Column("user_role_id"), ForeignKey(nameof(UserRole))]
    public long UserRoleId { get; set; }
    public virtual UserRole UserRole { get; set; } = default!;

    [Column("permission_id"), ForeignKey(nameof(Permission))]
    public long PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = default!;
}
