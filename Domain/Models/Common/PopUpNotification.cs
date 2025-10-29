using Common.Common.Models;
using Common.Models.Base;
using Domain.Enum.Notify;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Common;

[Table("pop_up_notification", Schema = "common")]
public class PopUpNotification : AuditableModelBase<long>
{
    public long UserId { get; set; }
    [StringLength(200)]
    public MultiLanguageField Title { get; set; } = default!;
    [StringLength(500)]
    public MultiLanguageField? Description { get; set; }
    [StringLength(500)] public string? ThumbUrl { get; set; }
    public EnumPopUpNotificationType Type { get; set; }

    [ForeignKey(nameof(Source))]
    public long? SourceId { get; set; }
    public PopUpNotificationSource Source { get; set; } = default!;
    public bool IsShown { get; set; }
    public DateTime? ShownAt { get; set; }
    public bool IsClicked { get; set; }
    public bool IsClosed { get; set; }
    public string? EventDataJson { get; set; }
}

