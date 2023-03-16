using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models.Entities;
using API.Values;

namespace API.Utilities;

public class ClaimsWrapperMapper : IClaimsWrapperMapper
{
    private readonly ICryptography cryptography;
    private readonly ILogger<ClaimsWrapperMapper> logger;

    public ClaimsWrapperMapper(ICryptography cryptography, ILogger<ClaimsWrapperMapper> logger)
    {
        this.cryptography = cryptography;
        this.logger = logger;
    }

    public ClaimsWrapper Map(User user, string accessToken, string identityToken) => new(cryptography)
    {
        Id = user.Id,
        ProviderId = user.ProviderId,
        Name = user.Name,
        Tin = user.Company.Tin,
        CompanyName = user.Company.Name,
        AcceptedTermsVersion = user.AcceptedTermsVersion,
        AllowCPRLookup = user.AllowCPRLookup,
        EncryptedAccessToken = cryptography.Encrypt(accessToken),
        EncryptedIdentityToken = cryptography.Encrypt(identityToken)
    };

    public ClaimsWrapper? Map(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            MissingProperty(nameof(user));
            return null;
        }

        var companyName = user.FindFirstValue(UserClaimName.CompanyName);
        if (companyName == null)
        {
            MissingProperty(nameof(UserClaimName.CompanyName));
            return null;
        }

        var tin = user.FindFirstValue(UserClaimName.Tin);
        if (tin == null)
        {
            MissingProperty(nameof(UserClaimName.Tin));
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
            Tin = tin,
            CompanyName = companyName,
            AcceptedTermsVersion = version,
            AllowCPRLookup = allowCPRLookup,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedIdentityToken = encryptedIdentityToken,
        };
    }

    private void MissingProperty(string name) => logger.LogWarning("Missing property: '{Property}'", name);
}
