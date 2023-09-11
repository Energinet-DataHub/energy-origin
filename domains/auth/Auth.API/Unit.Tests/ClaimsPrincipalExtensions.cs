using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Unit.Tests;

// FIXME: move and rename
public static class TestClaimsPrincipal
{
    public static ClaimsPrincipal Make(
        Guid? id = default,
        string? matchedRoles = default,
        string? encryptedIdentityToken = default,
        ProviderType providerType = ProviderType.MitIdPrivate
    )
    {
        var identity = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, "descriptor.Name"),
            new(JwtRegisteredClaimNames.Sub, "descriptor.Subject.ToString()"),
            new(UserClaimName.ProviderType, providerType.ToString()),
            new(UserClaimName.AllowCprLookup, "false"),
            new(UserClaimName.Actor, id?.ToString() ?? Guid.NewGuid().ToString()),
            new(UserClaimName.MatchedRoles, matchedRoles ?? ""),
            // new(UserClaimName.CompanyId, ""),
            // new(UserClaimName.CompanyName, ""),
            // new(UserClaimName.Tin, ""),
            new(UserClaimName.AccessToken, ""),
            new(UserClaimName.IdentityToken, encryptedIdentityToken ?? ""),
            new(UserClaimName.ProviderKeys, ""),
        };

        return new ClaimsPrincipal(new ClaimsIdentity(identity));
    }

    public static void PrepareUser(
        this ControllerBase controller,
        Guid? id = default,
        string? matchedRoles = default,
        string? encryptedIdentityToken = default,
        ProviderType providerType = ProviderType.MitIdPrivate
    ) => controller.ControllerContext = new()
    {
        HttpContext = new DefaultHttpContext()
        {
            User = Make(id: id, matchedRoles: matchedRoles, encryptedIdentityToken: encryptedIdentityToken, providerType: providerType)
        }
    };
}
