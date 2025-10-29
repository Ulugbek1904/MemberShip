using Common.Common.Models;
using Common.Models.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.References;

[Table("regions", Schema = "ref"), Index(nameof(Code))]
public class Region : AuditableModelBase<int>
{
    [Column("name")]
    public MultiLanguageField Name { get; set; } = default!;

    [MaxLength(5)]
    public string? Code { get; set; } = default;
}