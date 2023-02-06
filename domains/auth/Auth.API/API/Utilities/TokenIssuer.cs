using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public class TokenIssuer
{
    public static string Issue(ICryptography cryptography, TokenOptions options, Input input, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(options);

        var state = ResolveState(input.UserId);

        var descriptor = CreateTokenDescriptor(cryptography, options, credentials, input, state, issueAt ?? DateTime.UtcNow);

        return CreateToken(descriptor);
    }

    private static SigningCredentials CreateSigningCredentials(TokenOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));

        var key = new RsaSecurityKey(rsa);

        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    private static UserState ResolveState(string userId) => new(User: new User(Id: userId, Name: "Resolved users full name", Tin: "1234567890"), Scopes: "featureA featureB"); // FIXME: Resolve state using user service

    private static SecurityTokenDescriptor CreateTokenDescriptor(ICryptography cryptography, TokenOptions options, SigningCredentials credentials, Input input, UserState state, DateTime issueAt) => new()
    {
        Subject = new ClaimsIdentity(new[] {
            new Claim(JwtRegisteredClaimNames.Sub, state.User.Id),
            new Claim(JwtRegisteredClaimNames.Name, state.User.Name),
        }),
        NotBefore = issueAt,
        Expires = issueAt.Add(options.Duration),
        Issuer = options.Issuer,
        Audience = options.Audience,
        SigningCredentials = credentials,
        Claims = new Dictionary<string, object> {
            { UserClaim.Scopes, state.Scopes },
            { UserClaim.AccessToken, cryptography.Encrypt(input.AccessToken) },
            { UserClaim.IdentityToken, cryptography.Encrypt(input.IdentityToken) },
        }
    };

    private static string CreateToken(SecurityTokenDescriptor descriptor)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }

    public record Input(string UserId, string AccessToken, string IdentityToken);

    // FIXME: migrate usage of private records to actual user models

    private record UserState(User User, string Scopes);

    private record User(string Id, string Name, string? Tin);
}
