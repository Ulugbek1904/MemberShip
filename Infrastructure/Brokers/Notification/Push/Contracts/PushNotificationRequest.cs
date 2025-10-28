using Common.Common.Models;

namespace Infrastructure.Brokers.Notification.Push.Contracts;

public class PushNotificationRequest
{
    public MultiLanguageField Title { get; set; } = default!;
    public MultiLanguageField Description { get; set; } = default!;
    public string? ThumbUrl { get; set; }
    public long? SourceId { get; set; }
}
