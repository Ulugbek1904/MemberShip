using System.Text.Json;

namespace Common.Common.Helpers;

public class SerializerHelper
{
    public static string ToJsonString(object? data)
    {
        return ToJsonString(data, JsonSerializerOptions.Default);
    }

    public static string ToJsonString(object? data, JsonSerializerOptions options)
    {
        return JsonSerializer.Serialize(data, options);
    }

    public static T? FromJsonString<T>(string? content)
    {
        return FromJsonString<T>(content, JsonSerializerOptions.Default);
    }

    public static T? FromJsonString<T>(string? content, JsonSerializerOptions options)
    {
        if (!content.IsNullOrEmpty())
        {
            return JsonSerializer.Deserialize<T>(content);
        }

        return default(T);
    }
}
