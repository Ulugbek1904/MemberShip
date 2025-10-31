using Microsoft.IdentityModel.SecurityTokenService;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Domain.Extensions;

public static partial class StringExtension
{
    public static string GetCorrectPhoneNumber(this string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new BadRequestException("Phone_number_cannot_be_empty");

        phoneNumber = MyRegex().Replace(phoneNumber, "");

        if (phoneNumber.StartsWith("998") && phoneNumber.Length == 12)
            return phoneNumber;

        return phoneNumber.Length switch
        {
            9 => $"998{phoneNumber}",
            10 when phoneNumber.StartsWith($"8") => $"998{phoneNumber[1..]}",
            _ => throw new BadRequestException("Phone number must be 9 digits (local) or 12 digits starting with '998'")
        };
    }

    public static string Hash(this string input)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Production")
            return input;

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex MyRegex();
}
