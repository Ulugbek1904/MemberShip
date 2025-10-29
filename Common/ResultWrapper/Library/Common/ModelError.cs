using System.Text.Json.Serialization;

namespace Common.ResultWrapper.Library.Common;

public class ModelError
{
    public string Key { get; set; }

    public string? ErrorMessage { get; set; }

    [JsonConstructor]
    public ModelError(string key, string? errorMessage)
    {
        Key = key;
        ErrorMessage = errorMessage;
    }

    public ModelError()
    {
    }
}
