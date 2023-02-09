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
    private readonly IOptions<TermsOptions> termsOptions;
    private readonly IOptions<TokenOptions> tokenOptions;
    private readonly IUserService userService;

    public TokenIssuer(IOptions<TermsOptions> termsOptions, IOptions<TokenOptions> tokenOptions, IUserService userService)
    {
        this.termsOptions = termsOptions;
        this.tokenOptions = tokenOptions;
        this.userService = userService;
    }

    public async Task<string> IssueAsync(string userId, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(tokenOptions.Value);

        var state = await ResolveStateAsync(termsOptions.Value, userService, userId);

        var descriptor = CreateTokenDescriptor(tokenOptions.Value, credentials, state, issueAt ?? DateTime.UtcNow);

        return CreateToken(descriptor);
    }

    private static SigningCredentials CreateSigningCredentials(TokenOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));

        var key = new RsaSecurityKey(rsa);

        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    private static async Task<UserState> ResolveStateAsync(TermsOptions options, IUserService userService, string userId)
    {
        var user = await userService.GetUserByIdAsync(Guid.Parse(userId)) ?? throw new KeyNotFoundException($"User not found: {userId}");
        var scope = user.AcceptedTermsVersion == options.CurrentVersion ? "terms dashboard production meters certificates" : "terms";
        return new(user.Id.ToString(), user.Name, user.AcceptedTermsVersion, user.Tin, scope);
    }

    private static SecurityTokenDescriptor CreateTokenDescriptor(TokenOptions options, SigningCredentials credentials, UserState state, DateTime issueAt)
    {
        var claims = new Dictionary<string, object> {
            { "scope", state.Scope },
            { "terms", state.AcceptedVersion },
        };

        if (state.Tin != null)
        {
            claims.Add("tin", state.Tin);
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

    private record UserState(string Id, string Name, int AcceptedVersion, string? Tin, string Scope);
}
