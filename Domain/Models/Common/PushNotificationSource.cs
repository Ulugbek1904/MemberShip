using Common.Common.Models;
using Common.Models.Base;
using Domain.Enum.Admin;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Common;

public class PushNotificationSource : SoftDeletableAndAuditableModelBase<long>
{
    public MultiLanguageField Title { get; set; } = default!;
    public EnumAppVersionType DeviceType { get; set; }
    public MultiLanguageField Description { get; set; } = default!;
    [StringLength(500)] public string? ThumbUrl { get; set; }
}
