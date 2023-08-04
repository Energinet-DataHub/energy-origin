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

        if (!bool.TryParse(user.FindFirstValue(UserClaimName.AllowCprLookup), out var allowCprLookup))
        {
            MissingProperty(nameof(UserClaimName.AllowCprLookup));
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
        if (actor == null)
        {
            MissingProperty(nameof(UserClaimName.Actor));
            return null;
        }

        return new(cryptography)
        {
            Id = Guid.Parse(actor),
            ProviderType = providerType,
            Name = name,
            CompanyId = Guid.TryParse(user.FindFirstValue(UserClaimName.CompanyId), out var companyId) ? companyId : null,
            Tin = user.FindFirstValue(UserClaimName.Tin),
            CompanyName = user.FindFirstValue(UserClaimName.CompanyName),
            AcceptedPrivacyPolicyVersion = Convert.ToInt32(user.FindFirstValue(UserClaimName.AcceptedPrivacyPolicyVersion)),
            AcceptedTermsOfServiceVersion = Convert.ToInt32(user.FindFirstValue(UserClaimName.AcceptedTermsOfServiceVersion)),
            Roles = user.FindFirstValue(UserClaimName.Roles),
            AllowCprLookup = allowCprLookup,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedIdentityToken = encryptedIdentityToken,
            EncryptedProviderKeys = encryptedProviderKeys
        };
    }

    private void MissingProperty(string name) => logger.LogWarning("Missing property: '{Property}'", name);
}
