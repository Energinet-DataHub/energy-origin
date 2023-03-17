using API.Models.Entities;
using AuthLibrary.Utilities;

namespace API.Utilities;

public class UserDescriptMapper : UserDescriptMapperBase, IUserDescriptMapper
{
    private readonly ICryptography cryptography;
    private readonly ILogger<UserDescriptMapper> logger;

    public UserDescriptMapper(ICryptography cryptography, ILogger<UserDescriptMapper> logger) : base(cryptography, logger)
    {
        this.cryptography = cryptography;
        this.logger = logger;
    }

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
