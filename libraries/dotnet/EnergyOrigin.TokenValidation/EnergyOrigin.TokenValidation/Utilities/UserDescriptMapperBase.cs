using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Logging;

namespace EnergyOrigin.TokenValidation.Utilities;

public class UserDescriptMapperBase : IUserDescriptMapperBase
{
    private readonly ICryptography cryptography;
    private readonly ILogger<UserDescriptMapperBase> logger;

    public UserDescriptMapperBase(ICryptography cryptography, ILogger<UserDescriptMapperBase> logger)
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

        var providerId = user.FindFirstValue(UserClaimName.ProviderId);
        if (providerId == null)
        {
            MissingProperty(nameof(UserClaimName.ProviderId));
            return null;
        }

        var name = user.FindFirstValue(JwtRegisteredClaimNames.Name);
        if (name == null)
        {
            MissingProperty(nameof(JwtRegisteredClaimNames.Name));
            return null;
        }

        if (!int.TryParse(user.FindFirstValue(UserClaimName.CurrentTermsVersion), out var version))
        {
            MissingProperty(nameof(UserClaimName.CurrentTermsVersion));
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
            AcceptedCurrentTermsVersion = version,
            AllowCPRLookup = allowCPRLookup,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedIdentityToken = encryptedIdentityToken,
        };
    }

    private void MissingProperty(string name) => logger.LogWarning("Missing property: '{Property}'", name);
}
