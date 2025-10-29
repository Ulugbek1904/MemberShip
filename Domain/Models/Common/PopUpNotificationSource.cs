using Common.Common.Models;
using Common.Models.Base;
using Domain.Enum.Notify;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Common;

[Table("pop_up_notification_source", Schema = "common")]
public class PopUpNotificationSource : SoftDeletableAndAuditableModelBase<long>
{
    public MultiLanguageField Title { get; set; } = default!;
    public MultiLanguageField? Description { get; set; }
    public EnumPopUpNotificationType Type { get; set; }
    [StringLength(500)] public string? ThumbUrl { get; set; }
    public EnumPopUpTrigger Trigger { get; set; }
    public string[]? TriggerValue { get; set; }
    public int DisplayLimitPerUser { get; set; } = 1;
}
