using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Options;
using API.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public class TokenIssuer : ITokenIssuer
{
    private readonly TermsOptions termsOptions;
    private readonly TokenOptions tokenOptions;
    private readonly IUserService userService;

    public TokenIssuer(IOptions<TermsOptions> termsOptions, IOptions<TokenOptions> tokenOptions, IUserService userService)
    {
        this.termsOptions = termsOptions.Value;
        this.tokenOptions = tokenOptions.Value;
        this.userService = userService;
    }

    public async Task<string> IssueAsync(UserDescriptor descriptor, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(tokenOptions);

        var state = await ResolveStateAsync(termsOptions, userService, descriptor);

        return CreateToken(CreateTokenDescriptor(tokenOptions, credentials, descriptor, state, issueAt ?? DateTime.UtcNow));
    }

    private static SigningCredentials CreateSigningCredentials(TokenOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));

        var key = new RsaSecurityKey(rsa);

        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    private static async Task<UserState> ResolveStateAsync(TermsOptions options, IUserService userService, UserDescriptor descriptor)
    {
        var userId = descriptor.Id?.ToString();
        int version;
        if (userId == null)
        {
            version = descriptor.AcceptedTermsVersion;
        }
        else
        {
            var user = await userService.GetUserByIdAsync(Guid.Parse(userId)) ?? throw new KeyNotFoundException($"User not found: {userId}");
            version = user.AcceptedTermsVersion;
        }
        var scope = version == options.CurrentVersion ? "terms dashboard production meters certificates" : "terms";
        return new(userId, version, scope);
    }

    private static SecurityTokenDescriptor CreateTokenDescriptor(TokenOptions options, SigningCredentials credentials, UserDescriptor descriptor, UserState state, DateTime issueAt)
    {
        var claims = new Dictionary<string, object> {
            { UserClaimName.Scope, state.Scope },
            { UserClaimName.AccessToken, descriptor.EncryptedAccessToken },
            { UserClaimName.IdentityToken, descriptor.EncryptedIdentityToken },
            { UserClaimName.ProviderId, descriptor.ProviderId },
            { UserClaimName.TermsVersion, state.AcceptedVersion },
            { UserClaimName.AllowCPRLookup, descriptor.AllowCPRLookup },
        };
        if (descriptor.Tin != null)
        {
            claims.Add(UserClaimName.Tin, descriptor.Tin);
        }

        var identity = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Name, descriptor.Name)
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
