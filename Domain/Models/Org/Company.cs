using Common.Models.Base;
using Domain.Models.Auth;
using Domain.Models.References;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Org;

[Table("company", Schema = "org")]
public class Company : SoftDeletableAndAuditableModelBase<long>
{
    public required string Name { get; set; } = default!;
    [ForeignKey(nameof(Structure))]
    public long StructureId { get; set; }
    public Structure Structure { get; set; } = default!;

    [ForeignKey(nameof(DistrictId))]
    public int? DistrictId { get; set; } = 3;
    public District? District { get; set; }


    [ForeignKey(nameof(CompanyFilial))]
    public int? CompanyFilialId { get; set; }
    public ComanyFilial? CompanyFilial { get; set; }


    [ForeignKey(nameof(RegionId))]
    public int RegionId { get; set; }
    public Region Region { get; set; } = default!;

    public string? Address { get; set; }
    public string? DateOpen { get; set; }
    public string? BranchId { get; set; }
    public bool IsMobile { get; set; }
    public bool IsWeb { get; set; }
    public bool IsBlocked { get; set; }
    public ICollection<UserToCompany> UserCompanies { get; set; } = new List<UserToCompany>();
}