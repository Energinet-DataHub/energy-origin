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
}