using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace EnergyOrigin.TokenValidation.Utilities;

public class TokenSigner : ITokenSigner
{
    private readonly byte[] privateKeyPem;

    public TokenSigner(byte[] privateKeyPem)
    {
        this.privateKeyPem = privateKeyPem;
    }

    public string Sign(
        string subject,
        string name,
        string issuer,
        string audience,
        DateTime? issueAt = null,
        int duration = 120,
        IDictionary<string, object>? claims = null
    )
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(privateKeyPem));
        var key = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var identity = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, name),
            new(JwtRegisteredClaimNames.Sub, subject)
        };

        var issuedAt = issueAt ?? DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(identity),
            NotBefore = issuedAt,
            Expires = issuedAt.Add(TimeSpan.FromSeconds(duration)),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials,
            Claims = claims ?? ImmutableDictionary<string, object>.Empty
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }
}
