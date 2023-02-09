using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models;

namespace API.Utilities;

public class UserDescriptMapper : IUserDescriptMapper
{
    private readonly ICryptography cryptography;

    public UserDescriptMapper(ICryptography cryptography) => this.cryptography = cryptography;

    public UserDescriptor Map(User user, string accessToken, string identityToken) => new(
            user.Id,
            user.ProviderId,
            user.Name,
            user.Tin,
            user.AcceptedTermsVersion,
            user.AllowCPRLookup,
            cryptography.Encrypt(accessToken),
            cryptography.Encrypt(identityToken),
            cryptography);

    public UserDescriptor? Map(ClaimsPrincipal? user)
    {
        var id = user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "";
        Guid? userId = null;
        try
        {
            userId = Guid.Parse(id);
        }
        catch { }

        try
        {
            var identity = user?.Identity ?? throw new ArgumentNullException(nameof(user));

            return new(
                userId,
                ProviderId: user.FindFirstValue(UserClaimName.ProviderId) ?? throw new ArgumentNullException(UserClaimName.ProviderId, "-"),
                Name: identity.Name ?? throw new ArgumentNullException(nameof(identity.Name), "-"),
                Tin: user.FindFirstValue(UserClaimName.Tin) ?? throw new ArgumentNullException(UserClaimName.Tin, "-"),
                AcceptedTermsVersion: int.Parse(user.FindFirstValue(UserClaimName.TermsVersion) ?? throw new ArgumentNullException(UserClaimName.TermsVersion, "-")),
                AllowCPRLookup: bool.Parse(user.FindFirstValue(UserClaimName.AllowCPRLookup) ?? throw new ArgumentNullException(UserClaimName.AllowCPRLookup, "-")),
                EncryptedAccessToken: user.FindFirstValue(UserClaimName.AccessToken) ?? throw new ArgumentNullException(UserClaimName.AccessToken, "-"),
                EncryptedIdentityToken: user.FindFirstValue(UserClaimName.IdentityToken) ?? throw new ArgumentNullException(UserClaimName.IdentityToken, "-"),
                cryptography);
        }
        catch
        {
            return null;
        }
    }
}
