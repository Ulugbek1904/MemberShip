using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Models.Base;

public abstract class ModelBase<TId> where TId : struct
{
    [Column("id")]
    public TId Id { get; set; }
}
