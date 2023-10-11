using API.Models.Entities;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;

namespace API.Utilities;

public static class UserExtensions
{
    public static UserDescriptor MapDescriptor(this User user, ICryptography cryptography, ProviderType providerType, IEnumerable<string> matchedRoles, string accessToken, string identityToken)
    {
        OrganizationDescriptor? organization = null;
        if (user.Company != null)
        {
            organization = new OrganizationDescriptor()
            {
                Id = user.Company.Id,
                Name = user.Company.Name,
                Tin = user.Company.Tin
            };
        }

        return new UserDescriptor()
        {
            Id = user.Id ?? Guid.NewGuid(),
            ProviderType = providerType,
            Name = user.Name,
            Organization = organization,
            AllowCprLookup = user.AllowCprLookup,
            EncryptedAccessToken = cryptography.Encrypt(accessToken),
            EncryptedIdentityToken = cryptography.Encrypt(identityToken),
            EncryptedProviderKeys = cryptography.Encrypt(string.Join(" ", user.UserProviders.Select(x => $"{x.ProviderKeyType}={x.UserProviderKey}"))),
            MatchedRoles = string.Join(" ", matchedRoles),
        };
    }
}
