using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthLibrary.Values;

namespace AuthLibrary.Utilities;

public class UserDescriptMapperBase : IUserDescriptMapperBase
{
    private readonly ICryptography cryptography;

    public UserDescriptMapperBase(ICryptography cryptography)
    {
        this.cryptography = cryptography;
    }


    public UserDescriptor? Map(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return null;
        }

        var providerId = user.FindFirstValue(UserClaimName.ProviderId);
        if (providerId == null)
        {
            return null;
        }

        var name = user.FindFirstValue(JwtRegisteredClaimNames.Name);
        if (name == null)
        {
            return null;
        }

        if (!int.TryParse(user.FindFirstValue(UserClaimName.TermsVersion), out var version))
        {
            return null;
        }

        if (!bool.TryParse(user.FindFirstValue(UserClaimName.AllowCPRLookup), out var allowCPRLookup))
        {
            return null;
        }

        var encryptedAccessToken = user.FindFirstValue(UserClaimName.AccessToken);
        if (encryptedAccessToken == null)
        {
            return null;
        }

        var encryptedIdentityToken = user.FindFirstValue(UserClaimName.IdentityToken);
        if (encryptedIdentityToken == null)
        {
            return null;
        }

        var id = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        Guid? userId;
        if (id == null)
        {
            userId = null;
        }
        else if (Guid.TryParse(id, out var parsed))
        {
            userId = parsed;
        }
        else
        {
            return null;
        }

        return new(cryptography)
        {
            Id = userId,
            ProviderId = providerId,
            Name = name,
            Tin = user.FindFirstValue(UserClaimName.Tin),
            AcceptedTermsVersion = version,
            AllowCPRLookup = allowCPRLookup,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedIdentityToken = encryptedIdentityToken,
        };
    }
}
