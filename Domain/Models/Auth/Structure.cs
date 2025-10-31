using Common.Common.Models;
using Common.Models.Base;

namespace Domain.Models.Auth;

public class Structure : ReferenceModelBase<long>
{
    public MultiLanguageField Name { get; set; } = default!;
    public List<Permission> Permissions { get; set; } = default!;
    public bool IsDefault { get; set; }
}
