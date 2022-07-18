using System.Security.Cryptography;
using System.Text;

namespace Mock.Oidc.Extensions;

public static class StringExtensions
{
    public static string ToMd5(this string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
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