using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using AuthenticationScheme = EnergyOrigin.TokenValidation.b2c.AuthenticationScheme;

namespace EnergyOrigin.TokenValidation.Utilities;

public class UserDescriptor
{
    public required OrganizationDescriptor? Organization { get; set; }
    public required Guid Id { get; init; }
    public required ProviderType ProviderType { get; init; }
    public required string Name { get; init; }
    public required string MatchedRoles { get; init; }
    public required bool AllowCprLookup { get; init; }
    public required string EncryptedAccessToken { get; init; }
    public required string EncryptedIdentityToken { get; init; }
    // <summary>
    // The unencrypted data should follow this format: "ProviderKeyType1=ProviderKey1 ProviderKeyType2=ProviderKey2"
    // </summary>
    public required string EncryptedProviderKeys { get; init; }

    public Guid Subject => Organization?.Id ?? Id;

    public UserDescriptor() { }

    [SetsRequiredMembers]
    public UserDescriptor(HttpContext httpContext) : this(httpContext.User) { }

    [SetsRequiredMembers]
    public UserDescriptor(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            throw new PropertyMissingException(nameof(user));
        }

        if (Enum.TryParse<ProviderType>(user.FindFirstValue(UserClaimName.ProviderType), out var providerType))
        {
            ProviderType = providerType;
        }
        else
        {
            throw new PropertyMissingException(nameof(UserClaimName.ProviderType));
        }

        Name = user.FindFirstValue(JwtRegisteredClaimNames.Name) ?? throw new PropertyMissingException(nameof(JwtRegisteredClaimNames.Name));

        if (bool.TryParse(user.FindFirstValue(UserClaimName.AllowCprLookup), out var allowCprLookup))
        {
            AllowCprLookup = allowCprLookup;
        }
        else
        {
            throw new PropertyMissingException(nameof(UserClaimName.AllowCprLookup));
        }

        var actor = user.FindFirstValue(UserClaimName.Actor) ?? throw new PropertyMissingException(nameof(UserClaimName.Actor));

        Id = Guid.Parse(actor);

        MatchedRoles = user.FindFirstValue(UserClaimName.MatchedRoles) ?? string.Empty;

        EncryptedAccessToken = user.FindFirstValue(UserClaimName.AccessToken) ?? throw new PropertyMissingException(nameof(UserClaimName.AccessToken));
        EncryptedIdentityToken = user.FindFirstValue(UserClaimName.IdentityToken) ?? throw new PropertyMissingException(nameof(UserClaimName.IdentityToken));
        EncryptedProviderKeys = user.FindFirstValue(UserClaimName.ProviderKeys) ?? throw new PropertyMissingException(nameof(UserClaimName.ProviderKeys));

        var claimedOrganizationId = user.FindFirstValue(UserClaimName.OrganizationId);
        Guid.TryParse(claimedOrganizationId, out var organizationId);

        var organizationName = user.FindFirstValue(UserClaimName.OrganizationName);
        var tin = user.FindFirstValue(UserClaimName.Tin);

        if (claimedOrganizationId != null && organizationName != null && tin != null)
        {
            Organization = new()
            {
                Id = organizationId,
                Name = organizationName,
                Tin = tin
            };
        }
        else if (claimedOrganizationId != null || organizationName != null || tin != null)
        {
            throw new PropertyMissingException(nameof(Organization));
        }
    }
}
