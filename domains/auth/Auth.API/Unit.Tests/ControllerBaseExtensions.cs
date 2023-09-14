using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Unit.Tests;

public static class ControllerBaseExtensions
{
    public static void PrepareUser(
        this ControllerBase controller,
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
    ) => controller.ControllerContext = new()
    {
        HttpContext = new DefaultHttpContext()
        {
            User = TestClaimsPrincipal.Make(
                id: id,
                name: name,
                providerType: providerType,
                organization: organization,
                scope: scope,
                allowCprLookup: allowCprLookup,
                matchedRoles: matchedRoles,
                encryptedAccessToken: encryptedAccessToken,
                encryptedIdentityToken: encryptedIdentityToken,
                encryptedProviderKeys: encryptedProviderKeys
            )
        }
    };
}
