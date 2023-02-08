using System.Security.Claims;

namespace API.Utilities;

public static class ClaimsPrincipalExtensions
{
    public static string? AccessToken(this ClaimsPrincipal user, ICryptography cryptography)
    {
        var encrypted = user.FindFirst(UserClaimName.AccessToken)?.Value;
        return encrypted == null ? null : cryptography.Decrypt<string>(encrypted);
    }

    public static string? IdentityToken(this ClaimsPrincipal user, ICryptography cryptography)
    {
        var encrypted = user.FindFirst(UserClaimName.IdentityToken)?.Value;
        return encrypted == null ? null : cryptography.Decrypt<string>(encrypted);
    }
}
