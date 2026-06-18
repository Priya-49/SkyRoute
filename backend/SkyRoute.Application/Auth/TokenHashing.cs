using System.Security.Cryptography;
using System.Text;

namespace SkyRoute.Application.Auth;

internal static class TokenHashing
{
    public static string ComputeSha256(string rawToken)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }
}
