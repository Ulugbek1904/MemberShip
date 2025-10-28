using Common.Common.Models;
using Common.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Common;

[Table("push_notification", Schema = "common")]
public class PushNotification : AuditableModelBase<long>
{
    public long UserId { get; set; }
    public bool IsRead { get; set; }
    public MultiLanguageField? Title { get; set; }
    public MultiLanguageField? Description { get; set; }
    public string? ThumbUrl { get; set; }

    [ForeignKey(nameof(Source))]
    public long? SourceId { get; set; }
    public PushNotificationSource? Source { get; set; }
}
