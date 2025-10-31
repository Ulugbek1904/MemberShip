namespace Common.Web.Auth.ConfigurationModels;

public class JwtAuthConfiguration
{
    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    public required string SecretKey { get; set; }

    public required int ExpireTimeInMinutes { get; set; }
}