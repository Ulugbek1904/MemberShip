using Common.Common.Models;
using Common.Models.Base;
using System.Security;

namespace Domain.Models.Auth;

public class Structure : ReferenceModelBase<long>
{
    public MultiLanguageField Name { get; set; } = default!;
    public List<Permission> Permissions { get; set; } = default!;
    public bool IsDefault { get; set; }
}
