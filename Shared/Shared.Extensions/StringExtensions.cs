using System.Security.Cryptography;
using System.Text;

namespace Shared.Extensions;

public static class StringExtensions
{
    // Create SHA256 hash from input string
    public static string ToSha256(this string str)
    {
        using SHA256 sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var t in bytes)
        {
            sb.Append(t.ToString("x2"));
        }
        return sb.ToString();
    }
}