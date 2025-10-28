using Common.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Models.Base;

public abstract class ReferenceModelBase<T> : ModelBase<T> where T : struct
{
    [Column("group_name", TypeName = "jsonb")]
    public MultiLanguageField? GroupName { get; set; }

    [Column("is_system_defined")]
    public bool IsSystemDefined { get; set; }
}
