using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Unit.Tests;

// FIXME: move and rename
public static class TestClaimsPrincipal
{
    public static ClaimsPrincipal Make(
        Guid? id = default,
        OrganizationDescriptor? organization = default,
        string? scope = default,
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
            new(UserClaimName.Scope, scope ?? ""),
            new(UserClaimName.AccessToken, ""),
            new(UserClaimName.IdentityToken, encryptedIdentityToken ?? ""),
            new(UserClaimName.ProviderKeys, ""),
        };

        if (organization != null)
        {
            identity.Add(new(UserClaimName.CompanyId, organization.Id.ToString()));
            identity.Add(new(UserClaimName.CompanyName, organization.Name));
            identity.Add(new(UserClaimName.Tin, organization.Tin));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(identity));
    }

    public static void PrepareUser(
        this ControllerBase controller,
        Guid? id = default,
        OrganizationDescriptor? organization = default,
        string? scope = default,
        string? matchedRoles = default,
        string? encryptedIdentityToken = default,
        ProviderType providerType = ProviderType.MitIdPrivate
    ) => controller.ControllerContext = new()
    {
        HttpContext = new DefaultHttpContext()
        {
            User = Make(id: id, organization: organization, scope: scope, matchedRoles: matchedRoles, encryptedIdentityToken: encryptedIdentityToken, providerType: providerType)
        }
    };
}
