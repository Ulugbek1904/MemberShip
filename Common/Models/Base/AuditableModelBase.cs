using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Models.Base;

public abstract class AuditableModelBase<TId> : ModelBase<TId> where TId : struct
{
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
