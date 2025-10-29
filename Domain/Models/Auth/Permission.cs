using Common.Common.Models;
using Common.Models.Base;

namespace Domain.Models.Auth;

public class Permission : ReferenceModelBase<long>
{
    public MultiLanguageField Name { get; set; } = default!;
    public ICollection<Structure> Structures { get; set; } = default!;
}
