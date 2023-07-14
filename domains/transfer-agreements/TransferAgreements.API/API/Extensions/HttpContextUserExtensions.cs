using System.Security.Claims;

namespace API.Extensions;

public static class HttpContextUserExtensions
{
    public static string FindSubjectGuidClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("sub");
    public static string FindSubjectNameClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("cpn");
    public static string FindSubjectTinClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("tin");
    public static string FindActorGuidClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("atr");
    public static string FindActorNameClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("name");
    public static string FindIssuerClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("iss");
    public static string FindIssuedAtClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("iat");
    public static string FindAudienceClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("aud");
    public static string FindExpirationTimeClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("exp");
}
