using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Models.Entities;
using API.Options;
using API.Services;
using API.Values;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public class TokenIssuer : ITokenIssuer
{
    private readonly TermsOptions termsOptions;
    private readonly TokenOptions tokenOptions;
    private readonly IUserDescriptMapper mapper;

    public TokenIssuer(IOptions<TermsOptions> termsOptions, IOptions<TokenOptions> tokenOptions, IUserDescriptMapper mapper)
    {
        this.termsOptions = termsOptions.Value;
        this.tokenOptions = tokenOptions.Value;
        this.mapper = mapper;
    }

    public async Task<string> IssueAsync(User user, string accessToken, string identityToken, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(tokenOptions);

        var state = await ResolveStateAsync(termsOptions, user);

        return CreateToken(CreateTokenDescriptor(user, accessToken, identityToken, mapper, tokenOptions, credentials, state, issueAt ?? DateTime.UtcNow));
    }

    private static SigningCredentials CreateSigningCredentials(TokenOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));

        var key = new RsaSecurityKey(rsa);

        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    private static async Task<UserState> ResolveStateAsync(TermsOptions options, User user)
    {
        var version = user.AcceptedTermsVersion;

        var scope = version == options.CurrentVersion ? "terms dashboard production meters certificates" : "terms";
        return new(user.Id?.ToString(), version, scope);

    }

    private static SecurityTokenDescriptor CreateTokenDescriptor(User user, string accessToken, string identityToken, IUserDescriptMapper mapper, TokenOptions options, SigningCredentials credentials, UserState state, DateTime issueAt)
    {
        var descriptor = mapper.Map(user, accessToken, identityToken);

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
