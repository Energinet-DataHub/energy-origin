using System.Text;

namespace Tests.Extensions;

public static class JwtExtensions
{
    public static string GetJwtPayload(this string jwt)
    {
        var payload = jwt.Split('.')[1];
        var bytes = Convert.FromBase64String(payload);
        return Encoding.UTF8.GetString(bytes);
    }
}