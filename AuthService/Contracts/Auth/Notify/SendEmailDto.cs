using System.Text.Json.Serialization;

namespace AuthService.Contracts.Auth.Notify;

public class SendEmailDto
{
    public string Email { get; set; }
    public string? Hash { get; set; }
    [JsonIgnore]
    public int TemplateId { get; set; }
}
