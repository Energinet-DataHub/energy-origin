using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
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
        Guid? companyId = default,
        string? companyName = default,
        string? tin = default,
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
            new(UserClaimName.AccessToken, ""),
            new(UserClaimName.IdentityToken, encryptedIdentityToken ?? ""),
            new(UserClaimName.ProviderKeys, ""),
        };

        if (companyId != null)
        {
            identity.Add(new(UserClaimName.CompanyId, companyId.ToString()!));
        }
        if (companyName != null)
        {
            identity.Add(new(UserClaimName.CompanyName, companyName));
        }
        if (tin != null)
        {
            identity.Add(new(UserClaimName.Tin, tin));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(identity));
    }

    public static void PrepareUser(
        this ControllerBase controller,
        Guid? id = default,
        Guid? companyId = default,
        string? companyName = default,
        string? tin = default,
        string? matchedRoles = default,
        string? encryptedIdentityToken = default,
        ProviderType providerType = ProviderType.MitIdPrivate
    ) => controller.ControllerContext = new()
    {
        HttpContext = new DefaultHttpContext()
        {
            User = Make(id: id, companyId: companyId, companyName: companyName, tin: tin, matchedRoles: matchedRoles, encryptedIdentityToken: encryptedIdentityToken, providerType: providerType)
        }
    };
}
