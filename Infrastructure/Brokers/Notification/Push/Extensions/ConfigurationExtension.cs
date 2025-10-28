using Infrastructure.Brokers.Notification.Push.Contracts;
using Infrastructure.Brokers.Notification.Push.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Brokers.Notification.Push.Extensions;

public static class ConfigurationExtension
{
    public static IServiceCollection AddOneSignal(this IServiceCollection services)
    {
        services
            .AddOptions<OneSignalConfig>()
            .BindConfiguration("OneSignal")
            .ValidateOnStart()
            .PostConfigure(config =>
            {
                Log.Information(
                    "OneSignal: {0}",
                    JsonSerializer.Serialize(
                        config,
                        JsonSerializerOptions.Default)
                );
            });

        return services;
    }
    public static IServiceCollection ConfigureNotify(this IServiceCollection services)
    {
        services.AddSingleton<IPushNotificationQueue, PushNotificationQueue>();
        services.AddHostedService<PushNotificationBackgroundService>();

        return services;
    }
}
