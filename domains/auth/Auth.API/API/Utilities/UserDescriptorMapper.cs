using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models.Entities;
using API.Utilities.Interfaces;
using API.Values;

namespace API.Utilities;

public class UserDescriptorMapper : IUserDescriptorMapper
{
    private readonly ICryptography cryptography;
    private readonly ILogger<UserDescriptorMapper> logger;

    public UserDescriptorMapper(ICryptography cryptography, ILogger<UserDescriptorMapper> logger)
    {
        this.cryptography = cryptography;
        this.logger = logger;
    }

    public UserDescriptor Map(User user, ProviderType providerType, string accessToken, string identityToken) => new(cryptography)
    {
        Id = user.Id,
        ProviderType = providerType,
        Name = user.Name,
        CompanyId = user.CompanyId,
        Tin = user.Company?.Tin,
        CompanyName = user.Company?.Name,
        AcceptedTermsVersion = user.AcceptedTermsVersion,
        AllowCPRLookup = user.AllowCPRLookup,
        EncryptedAccessToken = cryptography.Encrypt(accessToken),
        EncryptedIdentityToken = cryptography.Encrypt(identityToken),
        // "ProviderKeyType1:ProviderKey1 ProviderKeyType2:ProviderKey2"
        EncryptedProviderKeys = cryptography.Encrypt(string.Join(" ", user.UserProviders.Select(x => $"{x.ProviderKeyType}:{x.UserProviderKey}")))
    };

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

        if (!int.TryParse(user.FindFirstValue(UserClaimName.TermsVersion), out var version))
        {
            MissingProperty(nameof(UserClaimName.TermsVersion));
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

        return new(cryptography)
        {
            Id = Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ? userId : null,
            ProviderType = providerType,
            Name = name,
            CompanyId = Guid.TryParse(user.FindFirstValue(UserClaimName.CompanyId), out var companyId) ? companyId : null,
            Tin = user.FindFirstValue(UserClaimName.Tin),
            CompanyName = user.FindFirstValue(UserClaimName.CompanyName),
            AcceptedTermsVersion = version,
            AllowCPRLookup = allowCPRLookup,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedIdentityToken = encryptedIdentityToken,
            EncryptedProviderKeys = encryptedProviderKeys
        };
    }

    private void MissingProperty(string name) => logger.LogWarning("Missing property: '{Property}'", name);
}
