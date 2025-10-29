using Common.Common.Models;

namespace AuthService.Contracts.Auth.Notify;

public record PushNotifyToOneUserDto : PushNotifyDto
{
    public required long UserId { get; set; }
}
public record PushNotifyToAllUsersDto : PushNotifyDto
{
    public NotifyFilterDto? Filter { get; set; }
}
public abstract record PushNotifyDto
{
    public long? SourceId { get; set; }
    public MultiLanguageField Title { get; set; } = default!;
    public MultiLanguageField Description { get; set; } = default!;
    public string? ThumbUrl { get; set; }
}
public sealed record NotifyFilterDto
{
    public long? CompanyId { get; set; }
    public long? StatusId { get; set; }
    public bool? IsDirector { get; set; }
    public int? DistrictId { get; set; }
    public int? RegionId { get; set; }
    public int? TypeClientId { get; set; }
    public int? BankFilialId { get; set; }
}