using System.Security.Cryptography;
using System.Text;

namespace Oidc.Mock.Extensions;

public static class StringExtensions
{
    public static string ToMd5(this string input)
    {
        var bytes = MD5.HashData(Encoding.ASCII.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public static string EncodeBase64(this string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(valueBytes);
    }

    public static string DecodeBase64(this string value)
    {
        var valueBytes = Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(valueBytes);
    }
}
