using System;
using System.Security.Claims;

namespace API.Shared.Extensions;

public static class HttpContextUserExtensions
{
    public static string FindSubjectGuidClaim(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("sub") ?? throw new InvalidOperationException("Subject GUID claim is missing.");

    public static string FindSubjectNameClaim(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("cpn") ?? throw new InvalidOperationException("Subject name claim is missing.");

    public static string FindSubjectTinClaim(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("tin") ?? throw new InvalidOperationException("Subject TIN claim is missing.");

    public static string FindActorGuidClaim(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("atr") ?? throw new InvalidOperationException("Actor GUID claim is missing.");

    public static string FindActorNameClaim(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("name") ?? throw new InvalidOperationException("Actor name claim is missing.");

    public static string FindIssuerClaim(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("iss") ?? throw new InvalidOperationException("Issuer claim is missing.");

    public static string FindIssuedAtClaim(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("iat") ?? throw new InvalidOperationException("Issued At claim is missing.");

    public static string FindAudienceClaim(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("aud") ?? throw new InvalidOperationException("Audience claim is missing.");

    public static string FindExpirationTimeClaim(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("exp") ?? throw new InvalidOperationException("Expiration Time claim is missing.");

}
