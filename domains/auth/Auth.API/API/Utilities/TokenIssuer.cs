using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Options;
using API.Values;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public class TokenIssuer : ITokenIssuer
{
    private readonly TermsOptions termsOptions;
    private readonly TokenOptions tokenOptions;

    public TokenIssuer(IOptions<TermsOptions> termsOptions, IOptions<TokenOptions> tokenOptions)
    {
        this.termsOptions = termsOptions.Value;
        this.tokenOptions = tokenOptions.Value;
    }

    public string Issue(ClaimsWrapper claimsWrapper, bool versionBypass = false, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(tokenOptions);

        var state = ResolveState(termsOptions, claimsWrapper, versionBypass);

        return CreateToken(CreateTokenDescriptor(tokenOptions, credentials, claimsWrapper, state, issueAt ?? DateTime.UtcNow));
    }

    private static SigningCredentials CreateSigningCredentials(TokenOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));

        var key = new RsaSecurityKey(rsa);

        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    private static UserState ResolveState(TermsOptions options, ClaimsWrapper claimsWrapper, bool versionBypass)
    {
        var version = claimsWrapper.AcceptedTermsVersion;

        var scope = version == options.CurrentVersion || versionBypass ? UserScopeClaim.AllAcceptedScopes : UserScopeClaim.NotAcceptedTerms;

        return new(claimsWrapper.Id?.ToString(), version, scope);
    }

    private static SecurityTokenDescriptor CreateTokenDescriptor(TokenOptions options, SigningCredentials credentials, ClaimsWrapper claimsWrapper, UserState state, DateTime issueAt)
    {
        var claims = new Dictionary<string, object> {
            { UserClaimName.Scope, state.Scope },
            { UserClaimName.AccessToken, claimsWrapper.EncryptedAccessToken },
            { UserClaimName.IdentityToken, claimsWrapper.EncryptedIdentityToken },
            { UserClaimName.ProviderId, claimsWrapper.ProviderId },
            { UserClaimName.TermsVersion, state.AcceptedVersion },
            { UserClaimName.AllowCPRLookup, claimsWrapper.AllowCPRLookup },
            { UserClaimName.Tin, claimsWrapper.Tin },
            { UserClaimName.CompanyName, claimsWrapper.CompanyName }
        };

        var identity = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Name, claimsWrapper.Name)
        };
        if (state.Id != null)
        {
            identity.Add(new Claim(JwtRegisteredClaimNames.Sub, state.Id));
        }

        return new()
        {
            Subject = new ClaimsIdentity(identity),
            NotBefore = issueAt,
            Expires = issueAt.Add(options.Duration),
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = credentials,
            Claims = claims
        };
    }

    private static string CreateToken(SecurityTokenDescriptor descriptor)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }

    private record UserState(string? Id, int AcceptedVersion, string Scope);
}
