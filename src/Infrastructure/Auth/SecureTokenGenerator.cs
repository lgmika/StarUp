using System.Security.Cryptography;
using System.Text;

namespace StartupConnect.Infrastructure.Auth;

public sealed class SecureTokenGenerator
{
    public string CreateToken(int byteLength = 64)
    {
        return ToBase64Url(RandomNumberGenerator.GetBytes(byteLength));
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

