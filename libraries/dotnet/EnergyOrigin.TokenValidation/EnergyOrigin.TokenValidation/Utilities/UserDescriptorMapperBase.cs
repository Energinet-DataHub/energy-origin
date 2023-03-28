using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Logging;

namespace EnergyOrigin.TokenValidation.Utilities;

public class UserDescriptorMapperBase : IUserDescriptorMapperBase
{
    private readonly ICryptography cryptography;
    private readonly ILogger<UserDescriptorMapperBase> logger;

    public UserDescriptorMapperBase(ICryptography cryptography, ILogger<UserDescriptorMapperBase> logger)
    {
        this.cryptography = cryptography;
        this.logger = logger;
    }

    public UserDescriptor? Map(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            MissingProperty(nameof(user));
            return null;
        }

        if (Enum.TryParse<ProviderType>(user.FindFirstValue(UserClaimName.ProviderType), out var providerType) == false)
        {
            MissingProperty(nameof(UserClaimName.ProviderType));
            return null;
        }

        var name = user.FindFirstValue(JwtRegisteredClaimNames.Name);
        if (name == null)
        {
            MissingProperty(nameof(JwtRegisteredClaimNames.Name));
            return null;
        }

        if (!int.TryParse(user.FindFirstValue(UserClaimName.CurrentTermsVersion), out var currentVersion))
        {
            MissingProperty(nameof(UserClaimName.CurrentTermsVersion));
            return null;
        }

        if (!int.TryParse(user.FindFirstValue(UserClaimName.AcceptedTermsVersion), out var acceptedVersion))
        {
            MissingProperty(nameof(UserClaimName.AcceptedTermsVersion));
            return null;
        }

        if (!bool.TryParse(user.FindFirstValue(UserClaimName.AllowCPRLookup), out var allowCPRLookup))
        {
            MissingProperty(nameof(UserClaimName.AllowCPRLookup));
            return null;
        }

        var encryptedAccessToken = user.FindFirstValue(UserClaimName.AccessToken);
        if (encryptedAccessToken == null)
        {
            MissingProperty(nameof(UserClaimName.AccessToken));
            return null;
        }

        var encryptedIdentityToken = user.FindFirstValue(UserClaimName.IdentityToken);
        if (encryptedIdentityToken == null)
        {
            MissingProperty(nameof(UserClaimName.IdentityToken));
            return null;
        }

        var encryptedProviderKeys = user.FindFirstValue(UserClaimName.ProviderKeys);
        if (encryptedProviderKeys == null)
        {
            MissingProperty(nameof(UserClaimName.ProviderKeys));
            return null;
        }

        var actor = user.FindFirstValue(UserClaimName.Actor);

        return new(cryptography)
        {
            Id = actor is not null ? Guid.Parse(actor) : null,
            ProviderType = providerType,
            Name = name,
            CompanyId = Guid.TryParse(user.FindFirstValue(UserClaimName.CompanyId), out var companyId) ? companyId : null,
            Tin = user.FindFirstValue(UserClaimName.Tin),
            CompanyName = user.FindFirstValue(UserClaimName.CompanyName),
            AcceptedTermsVersion = acceptedVersion,
            CurrentTermsVersion = currentVersion,
            AllowCPRLookup = allowCPRLookup,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedIdentityToken = encryptedIdentityToken,
            EncryptedProviderKeys = encryptedProviderKeys
        };
    }

    private void MissingProperty(string name) => logger.LogWarning("Missing property: '{Property}'", name);
}
