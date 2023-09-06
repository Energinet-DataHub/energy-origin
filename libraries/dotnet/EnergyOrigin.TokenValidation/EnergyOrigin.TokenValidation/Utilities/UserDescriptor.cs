using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Diagnostics.CodeAnalysis;
using EnergyOrigin.TokenValidation.Values;

namespace EnergyOrigin.TokenValidation.Utilities;

public class UserDescriptor
{
    public required OrganizationDescriptor? Organization { get; init; }
    public required Guid Id { get; init; }
    public required ProviderType ProviderType { get; init; }
    public required string Name { get; init; }
    public required string MatchedRoles { get; init; }
    public required bool AllowCprLookup { get; init; }
    public required string EncryptedAccessToken { get; init; }
    public required string EncryptedIdentityToken { get; init; }
    // <summary>
    // The unencrypted data should follow this format: "ProviderKeyType1:ProviderKey1 ProviderKeyType2=ProviderKey2"
    // </summary>
    public required string EncryptedProviderKeys { get; init; }

    public Guid Subject => Organization?.Id ?? Id;

    public UserDescriptor() { }

    [SetsRequiredMembers]
    public UserDescriptor(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            throw new PropertyMissingException(nameof(user));
        }

        if (Enum.TryParse<ProviderType>(user.FindFirstValue(UserClaimName.ProviderType), out var providerType) == false)
        {
            throw new PropertyMissingException(nameof(UserClaimName.ProviderType));
        }

        Name = user.FindFirstValue(JwtRegisteredClaimNames.Name) ?? throw new PropertyMissingException(nameof(JwtRegisteredClaimNames.Name));

        if (!bool.TryParse(user.FindFirstValue(UserClaimName.AllowCprLookup), out var allowCprLookup))
        {
            AllowCprLookup = allowCprLookup;
        }
        else
        {
            throw new PropertyMissingException(nameof(UserClaimName.AllowCprLookup));
        }

        var actor = user.FindFirstValue(UserClaimName.Actor);
        if (actor == null)
        {
            throw new PropertyMissingException(nameof(UserClaimName.Actor));
        }

        Id = Guid.Parse(actor);

        MatchedRoles = user.FindFirstValue(UserClaimName.MatchedRoles) ?? string.Empty;

        Guid.TryParse(user.FindFirstValue(UserClaimName.CompanyId), out var organizationId);
        var organizationName = user.FindFirstValue(UserClaimName.CompanyName);
        var tin = user.FindFirstValue(UserClaimName.Tin);

        if (organizationId != null && organizationName != null && tin != null)
        {
            Organization = new()
            {
                Id = organizationId,
                Name = organizationName,
                Tin = tin
            };
        }
        else if (organizationId != null || organizationName != null || tin != null)
        {
            throw new PropertyMissingException(nameof(Organization));
        }
    }
}

public class PropertyMissingException : Exception
{
    public PropertyMissingException(string parameter) : base($"Missing property: $parmeter") { }
}
