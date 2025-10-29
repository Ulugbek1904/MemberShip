using Common.Common.Models;
using Common.Models.Base;

namespace Domain.Models.References;

public class ComanyFilial : ReferenceModelBase<long>
{
    public MultiLanguageField CompanyName { get; set; } = default!;
    public MultiLanguageField BranchName { get; set; } = default!;
    public required string BranchId { get; set; }
    public string? OpeningDateOfBranch { get; set; }
    public MultiLanguageField? Located { get; set; }
    public MultiLanguageField Address { get; set; } = default!;
    public string? GeoLocalizationCoordinates { get; set; }
    public string RegionCode { get; set; } = "";
}
