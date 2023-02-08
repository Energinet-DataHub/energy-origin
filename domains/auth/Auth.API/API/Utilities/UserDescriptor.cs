using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Utilities;

public class UserDescriptor
{
    private readonly ICryptography cryptography;
    private readonly string accessToken;
    private readonly string identityToken;

    public UserDescriptor(ICryptography cryptography, ClaimsPrincipal? user)
    {
        this.cryptography = cryptography;

        var identity = user?.Identity ?? throw new ArgumentNullException(nameof(user));

        var id = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "";
        Guid? userId = null;
        try
        {
            userId = Guid.Parse(id);
        }
        catch { }

        accessToken = user.FindFirstValue(UserClaimName.AccessToken) ?? throw new ArgumentNullException(UserClaimName.AccessToken, "-");
        identityToken = user.FindFirstValue(UserClaimName.IdentityToken) ?? throw new ArgumentNullException(UserClaimName.IdentityToken, "-");

        Name = identity.Name ?? throw new ArgumentNullException(nameof(identity.Name), "-");
        ProviderId = user.FindFirstValue(UserClaimName.Tin) ?? throw new ArgumentNullException(UserClaimName.ProviderId, "-");
        Tin = user.FindFirstValue(UserClaimName.Tin) ?? throw new ArgumentNullException(UserClaimName.Tin, "-");
        Id = userId;
        AcceptedTermsVersion = int.Parse(user.FindFirstValue(UserClaimName.TermsVersion) ?? throw new ArgumentNullException(UserClaimName.TermsVersion, "-"));
        AllowCPRLookup = bool.Parse(user.FindFirstValue(UserClaimName.AllowCPRLookup) ?? throw new ArgumentNullException(UserClaimName.AllowCPRLookup, "-"));
    }

    public Guid? Id { get; init; }
    public string ProviderId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public int AcceptedTermsVersion { get; init; }
    public string? Tin { get; init; }
    public bool AllowCPRLookup { get; init; }
    public string? AccessToken => cryptography.Decrypt<string>(accessToken);
    public string? IdentityToken => cryptography.Decrypt<string>(identityToken);
}
