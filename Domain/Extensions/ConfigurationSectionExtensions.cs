using Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Domain.Extensions;

public static class ConfigurationSectionExtensions
{
    public static string GetValueOrThrowsNotFound(this IConfigurationSection section, string key)
    {
        var value = section[key];
        if (value.IsNullOrEmpty())
            throw new NotFoundException($"{key} is not found in section {section.Key}");

        return value!;
    }

    public static T GetValueOrThrowsNotFound<T>(this IConfigurationSection section, string key)
    {
        return (T)Convert.ChangeType(section.GetValueOrThrowsNotFound(key), typeof(T));
    }
}
