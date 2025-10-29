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
    public Country CountryId { get; set; } = default!;
    public bool IsMobile { get; set; }
    public bool IsWeb { get; set; }
    public bool IsBlocked { get; set; }
    public IQueryable<ComanyFilial>? CompanyFilials { get; set; }
    public ICollection<UserToCompany> UserCompanies { get; set; } = new List<UserToCompany>();
}