using API.Models.Entities;
using EnergyOrigin.TokenValidation.Utilities;

namespace API.Utilities;

public class UserDescriptMapper : UserDescriptMapperBase, IUserDescriptMapper
{
    private readonly ICryptography cryptography;

    public UserDescriptMapper(ICryptography cryptography, ILogger<UserDescriptMapper> logger) : base(cryptography, logger) => this.cryptography = cryptography;

    public UserDescriptor Map(User user, string accessToken, string identityToken) => new(cryptography)
    {
        Id = user.Id,
        ProviderId = user.ProviderId,
        Name = user.Name,
        Tin = user.Tin,
        AcceptedTermsVersion = user.AcceptedTermsVersion,
        AllowCPRLookup = user.AllowCPRLookup,
        EncryptedAccessToken = cryptography.Encrypt(accessToken),
        EncryptedIdentityToken = cryptography.Encrypt(identityToken)
    };
}
