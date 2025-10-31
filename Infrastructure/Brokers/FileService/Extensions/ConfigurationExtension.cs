using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text.Json;

namespace Infrastructure.Brokers.FileService.Extensions;

public static class ConfigurationExtension
{
    public static IServiceCollection AddFileBroker(this IServiceCollection services)
    {
        services
            .AddOptions<FileConfig>()
            .BindConfiguration("FileService")
            .ValidateOnStart()
            .PostConfigure(config =>
            {
                Log.Information(
                    "File: {0}",
                    JsonSerializer.Serialize(
                        config,
                        JsonSerializerOptions.Default)
                );
            });

        return services;
    }
}
