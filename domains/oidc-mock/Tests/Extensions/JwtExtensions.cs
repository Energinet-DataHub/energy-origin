using System.Text;

namespace Tests.Extensions;

public static class JwtExtensions
{
    public static string GetJwtPayload(this string jwt)
    {
        var payload = jwt.Split('.')[1];
        var paddedPayload = $"{payload}{GetPadding(payload)}";
        var bytes = Convert.FromBase64String(paddedPayload);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string GetPadding(string base64EncodedString) =>
        (base64EncodedString.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty
        };
}
