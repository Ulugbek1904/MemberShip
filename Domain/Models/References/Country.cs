using Common.Common.Models;
using Common.Models.Base;

namespace Domain.Models.References;

public class Country : SoftDeletableAndAuditableModelBase<int>
{
    public MultiLanguageField Name { get; set; }
}
