using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace API.Extensions;

public static class HttpContextUserExtensions
{
    public static string? FindSubjectClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("sub");
    public static string? FindActorClaim(this ClaimsPrincipal principal) => principal.FindFirstValue("atr");
}
