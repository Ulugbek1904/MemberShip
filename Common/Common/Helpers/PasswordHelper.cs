using System.Security.Cryptography;
using System.Text;

namespace Common.Common.Helpers;

public class PasswordHelper
{
    private const int KeySize = 32;

    private const int IterationsCount = 1000;

    public static string Encrypt(string password)
    {
        if (EnvironmentHelper.IsDevelopment)
        {
            return password;
        }

        using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(password), 1000, HashAlgorithmName.SHA256);
        return Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(32));
    }

    public static bool Verify(string passwordHash, string password)
    {
        if (EnvironmentHelper.IsDevelopment)
        {
            return passwordHash == password;
        }

        return Encrypt(password).SequenceEqual(passwordHash);
    }

    public static int GenerateRandom6DigitNumber()
    {
        return Random.Shared.Next(100000, 999999);
    }

    public static string GenerateRandom6LengthPassword()
    {
        return GenerateRandomNLengthPassword(6);
    }

    public static string GenerateRandomNLengthPassword(int n)
    {
        string text = "abcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            stringBuilder.Append(text[Random.Shared.Next(0, 35)]);
        }

        return stringBuilder.ToString();
    }
}