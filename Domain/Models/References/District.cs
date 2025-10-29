using Common.Common.Models;
using Common.Models.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.References;

[Table("districts", Schema = "ref"), Index(nameof(Code))]
public class District : AuditableModelBase<int>
{
    [Column("name")]
    public MultiLanguageField Name { get; set; } = default!;

    [MaxLength(5)]
    public string? Code { get; set; }

    public int RegionId { get; set; }
    public Region Region { get; set; } = default!;

}
