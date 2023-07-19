using API.Models.Entities;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;

namespace API.Utilities;

public class UserDescriptorMapper : UserDescriptorMapperBase, IUserDescriptorMapper
{
    private readonly ICryptography cryptography;

    public UserDescriptorMapper(ICryptography cryptography, ILogger<UserDescriptorMapper> logger) : base(cryptography, logger) => this.cryptography = cryptography;

    public UserDescriptor Map(User user, ProviderType providerType, string accessToken, string identityToken) => new(cryptography)
    {
        Id = user.Id ?? Guid.NewGuid(),
        ProviderType = providerType,
        Name = user.Name,
        CompanyId = user.CompanyId,
        Tin = user.Company?.Tin,
        CompanyName = user.Company?.Name,
        AllowCprLookup = user.AllowCprLookup,
        EncryptedAccessToken = cryptography.Encrypt(accessToken),
        EncryptedIdentityToken = cryptography.Encrypt(identityToken),
        EncryptedProviderKeys = cryptography.Encrypt(string.Join(" ", user.UserProviders.Select(x => $"{x.ProviderKeyType}={x.UserProviderKey}"))),
        UserStored = user.Id != null,
        Roles = string.Join(" ", user.Roles.Select(x => x.Key)),
        AcceptedPrivacyPolicyVersion = user.UserTerms.FirstOrDefault(x=>x.Type == UserTermsType.PrivacyPolicy)?.AcceptedVersion,
        AcceptedTermsOfServiceVersion = user.Company?.CompanyTerms.FirstOrDefault(x=>x.Type == CompanyTermsType.TermsOfService)?.AcceptedVersion
    };
}
