using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Models.Base;

public abstract class SoftDeletableAndAuditableModelBase<TId> : AuditableModelBase<TId> where TId : struct
{
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
