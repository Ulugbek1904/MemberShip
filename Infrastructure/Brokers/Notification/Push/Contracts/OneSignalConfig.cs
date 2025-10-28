namespace Infrastructure.Brokers.Notification.Push.Contracts;

public class OneSignalConfig
{
    public string Url { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string AppId { get; set; } = default!;
}
