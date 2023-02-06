using System.Security.Claims;

namespace API.Utilities;

public static class ClaimsPrincipalExtensions
{
    public static string? AccessToken(this ClaimsPrincipal user, ICryptography cryptography)
    {
        var encrypted = user.FindFirst(UserClaim.AccessToken)?.Value;
        return encrypted == null ? null : cryptography.Decrypt<string>(encrypted);
    }

    public static string? IdentityToken(this ClaimsPrincipal user, ICryptography cryptography)
    {
        var encrypted = user.FindFirst(UserClaim.IdentityToken)?.Value;
        return encrypted == null ? null : cryptography.Decrypt<string>(encrypted);
    }
}
