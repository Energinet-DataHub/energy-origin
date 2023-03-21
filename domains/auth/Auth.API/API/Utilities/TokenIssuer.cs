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

    public string Issue(UserDescriptor descriptor, bool versionBypass = false, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(tokenOptions);

        var state = ResolveState(termsOptions, descriptor, versionBypass);

        return CreateToken(CreateTokenDescriptor(tokenOptions, credentials, descriptor, state, issueAt ?? DateTime.UtcNow));
    }

    private static SigningCredentials CreateSigningCredentials(TokenOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));

        var key = new RsaSecurityKey(rsa);

        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    private static UserState ResolveState(TermsOptions options, UserDescriptor descriptor, bool versionBypass)
    {
        var version = descriptor.AcceptedTermsVersion;

        var scope = version == options.CurrentVersion || versionBypass ? UserScopeClaim.AllAcceptedScopes : UserScopeClaim.NotAcceptedTerms;

        return new(descriptor.Id?.ToString(), version, scope);
    }

    private static SecurityTokenDescriptor CreateTokenDescriptor(TokenOptions options, SigningCredentials credentials, UserDescriptor descriptor, UserState state, DateTime issueAt)
    {
        var claims = new Dictionary<string, object> {
            { UserClaimName.Scope, state.Scope },
            { UserClaimName.AccessToken, descriptor.EncryptedAccessToken },
            { UserClaimName.IdentityToken, descriptor.EncryptedIdentityToken },
            { UserClaimName.ProviderId, descriptor.ProviderId },
            { UserClaimName.TermsVersion, state.AcceptedVersion },
            { UserClaimName.AllowCPRLookup, descriptor.AllowCPRLookup }
        };

        if (descriptor.Tin is not null) claims.Add(UserClaimName.Tin, descriptor.Tin);
        if (descriptor.CompanyName is not null) claims.Add(UserClaimName.CompanyName, descriptor.CompanyName);

        var identity = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Name, descriptor.Name)
        };
        if (state.Id != null)
        {
            identity.Add(new Claim(JwtRegisteredClaimNames.Sub, state.Id));

            claims.Add(UserClaimName.Subject, descriptor.CompanyId?.ToString() ?? state.Id);
            claims.Add(UserClaimName.Actor, state.Id);
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
