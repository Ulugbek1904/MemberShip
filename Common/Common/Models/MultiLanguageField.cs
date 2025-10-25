using System.Text.Json.Serialization;

namespace Common.Common.Models;

public class MultiLanguageField
{
    [JsonPropertyName("uz")]
    public string Uz { get; set; }

    [JsonPropertyName("ru")]
    public string Ru { get; set; }

    [JsonPropertyName("eng")]
    public string Eng { get; set; }

    [JsonPropertyName("cyrl")]
    public string Cyrl { get; set; }

    protected bool Equals(MultiLanguageField other)
    {
        if (Uz == other.Uz && Ru == other.Ru && Eng == other.Eng)
        {
            return Cyrl == other.Cyrl;
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (this == obj)
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((MultiLanguageField)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Uz, Ru, Eng, Cyrl);
    }

    public static implicit operator MultiLanguageField(string data)
    {
        return new MultiLanguageField
        {
            Ru = data,
            Uz = data,
            Eng = data,
            Cyrl = data
        };
    }

    public static bool operator ==(MultiLanguageField a, string b)
    {
        if (!(a.Ru == b) && !(a.Eng == b) && !(a.Uz == b))
        {
            return a.Cyrl == b;
        }

        return true;
    }

    public static bool operator !=(MultiLanguageField a, string b)
    {
        if (a.Ru != b && a.Eng != b && a.Uz != b)
        {
            return a.Cyrl != b;
        }

        return false;
    }

    public override string ToString()
    {
        return SerializerHelper.ToJsonString(this);
    }
}