using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;

namespace Unit.Tests;

public static class TestClaimsPrincipal
{
    public static ClaimsPrincipal Make(
        Guid? id = default,
        string? name = default,
        ProviderType providerType = ProviderType.MitIdPrivate,
        OrganizationDescriptor? organization = default,
        string? scope = default,
        string? allowCprLookup = default,
        string? matchedRoles = default,
        string? encryptedAccessToken = default,
        string? encryptedIdentityToken = default,
        string? encryptedProviderKeys = default
    )
    {
        var identity = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, name ?? ""),
            new(JwtRegisteredClaimNames.Sub, id?.ToString() ?? Guid.NewGuid().ToString()),
            new(UserClaimName.ProviderType, providerType.ToString()),
            new(UserClaimName.AllowCprLookup, allowCprLookup ?? "false"),
            new(UserClaimName.Actor, id?.ToString() ?? Guid.NewGuid().ToString()),
            new(UserClaimName.MatchedRoles, matchedRoles ?? ""),
            new(UserClaimName.Scope, scope ?? ""),
            new(UserClaimName.AccessToken, encryptedAccessToken ?? ""),
            new(UserClaimName.IdentityToken, encryptedIdentityToken ?? ""),
            new(UserClaimName.ProviderKeys, encryptedProviderKeys ?? ""),
        };

        if (organization != null)
        {
            identity.Add(new(UserClaimName.OrganizationId, organization.Id.ToString()));
            identity.Add(new(UserClaimName.OrganizationName, organization.Name));
            identity.Add(new(UserClaimName.Tin, organization.Tin));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(identity));
    }
}
