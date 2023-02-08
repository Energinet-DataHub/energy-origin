using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Options;
using API.Services;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public class TokenIssuer : ITokenIssuer
{
    private readonly TermsOptions termsOptions;
    private readonly TokenOptions tokenOptions;
    private readonly ICryptography cryptography;
    private readonly IUserService userService;

    public TokenIssuer(TermsOptions termsOptions, TokenOptions tokenOptions, ICryptography cryptography, IUserService userService)
    {
        this.termsOptions = termsOptions;
        this.tokenOptions = tokenOptions;
        this.cryptography = cryptography;
        this.userService = userService;
    }

    public async Task<string> IssueAsync(string userId, string accessToken, string identityToken, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(tokenOptions);

        var state = await ResolveStateAsync(termsOptions, userService, userId, accessToken, identityToken);

        var descriptor = CreateTokenDescriptor(tokenOptions, credentials, state, issueAt ?? DateTime.UtcNow);

        return CreateToken(descriptor);
    }

    private static SigningCredentials CreateSigningCredentials(TokenOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));

        var key = new RsaSecurityKey(rsa);

        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    private static async Task<UserState> ResolveStateAsync(TermsOptions options, IUserService userService, string userId, string accessToken, string identityToken)
    {
        var user = await userService.GetUserByIdAsync(Guid.Parse(userId)) ?? throw new KeyNotFoundException($"User not found: {userId}");
        var scope = user.AcceptedTermsVersion == options.CurrentVersion ? "terms dashboard production meters certificates" : "terms";
        return new(user.Id.ToString(), user.Name, user.AcceptedTermsVersion, user.Tin, scope, accessToken, identityToken);
    }

    private SecurityTokenDescriptor CreateTokenDescriptor(TokenOptions options, SigningCredentials credentials, UserState state, DateTime issueAt)
    {
        var claims = new Dictionary<string, object> {
            { UserClaimName.Scope, state.Scope },
            { UserClaimName.AccessToken, cryptography.Encrypt(state.AccessToken) },
            { UserClaimName.IdentityToken, cryptography.Encrypt(state.IdentityToken) },
        };

        if (state.Tin != null)
        {
            claims.Add(UserClaimName.Tin, state.Tin);
        }

        return new()
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim(JwtRegisteredClaimNames.Sub, state.Id),
                new Claim(JwtRegisteredClaimNames.Name, state.Name),
            }),
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

    private record UserState(string Id, string Name, int AcceptedVersion, string? Tin, string Scope, string AccessToken, string IdentityToken);
}
