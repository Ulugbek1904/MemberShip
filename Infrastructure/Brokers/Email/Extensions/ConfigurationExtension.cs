using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Brokers.Email.Extensions;

public static class ConfigurationExtension
{
    public static IServiceCollection AddMailBroker(this IServiceCollection services)
    {
        services.AddOptions<MailConfig>()
            .BindConfiguration("Mail")
            .ValidateOnStart();

        return services;
    }
}