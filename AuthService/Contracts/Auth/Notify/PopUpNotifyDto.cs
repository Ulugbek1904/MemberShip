using Common.Common.Models;
using Domain.Enum.Notify;

namespace AuthService.Contracts.Auth.Notify;

public record PopUpNotifyDto
{
    public long UserId { get; set; }
    public long SourceId { get; set; }
    public required MultiLanguageField Title { get; set; }
    public MultiLanguageField? Description { get; set; }
    public EnumPopUpNotificationType Type { get; set; }
    public string? ThumbUrl { get; set; }
    public int DisplayLimitPerUser { get; set; } = 1;
}

public record ActivePopupDto
{
    public long SourceId { get; set; }
    public required MultiLanguageField Title { get; set; }
    public MultiLanguageField? Description { get; set; }
    public string? ThumbUrl { get; set; }
    public EnumPopUpNotificationType Type { get; set; }
    public EnumPopUpTrigger Trigger { get; set; }
    public string[]? TriggerPayload { get; set; }
    public int DisplayLimitPerUser { get; set; }
}

public record PopupEventDto
{
    public long SourceId { get; set; }
    public long UserId { get; set; }
    public string EventType { get; set; } = default!;
    public object? EventData { get; set; }
}

public record PopUpMetricsDto
{
    public long SourceId { get; set; }
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public int Closes { get; set; }
    public int FormSubmissions { get; set; }
}

public record MetriksRequest
{
    public long SourceId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public record ActivePopUpRequestDto
{
    public bool? IsOnRoute { get; set; }
}