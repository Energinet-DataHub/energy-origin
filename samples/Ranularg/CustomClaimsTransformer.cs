using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Ranularg;

public class CustomClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var claimsIdentity = (ClaimsIdentity)principal.Identity;

        // Log the claims
        foreach (var claim in claimsIdentity.Claims)
        {
            // Log or handle each claim
            Console.WriteLine($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }

        return Task.FromResult(principal);
    }
}
