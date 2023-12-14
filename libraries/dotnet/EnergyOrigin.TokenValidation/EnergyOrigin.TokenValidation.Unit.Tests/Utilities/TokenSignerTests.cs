using EnergyOrigin.TokenValidation.Utilities;
using System.IdentityModel.Tokens.Jwt;

namespace EnergyOrigin.TokenValidation.Unit.Tests.Utilities;

public class TokenSignerTests
{
    private readonly byte[] privateKeyPem = RsaKeyGenerator.GenerateTestKey();

    [Fact]
    public void Sign_ShouldGenerateToken_WithCorrectClaims()
    {
        var signer = new TokenSigner(privateKeyPem);
        var subject = "TestSubject";
        var name = "TestName";
        var issuer = "TestIssuer";
        var audience = "TestAudience";

        var tokenString = signer.Sign(subject, name, issuer, audience);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal(subject, token.Subject);
        Assert.Equal(name, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Equal(issuer, token.Issuer);
        Assert.Equal(audience, token.Audiences.First());
    }

    [Fact]
    public void Sign_ShouldSetCorrectExpiry_WhenDurationIsProvided()
    {
        var signer = new TokenSigner(privateKeyPem);
        var duration = 120;
        var issueAt = DateTime.UtcNow;
        var expectedExpiry = issueAt.AddSeconds(duration);

        var tokenString = signer.Sign("subject", "name", "issuer", "audience", issueAt, duration);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal(expectedExpiry, token.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Sign_WithNullIssueAt_ShouldDefaultToCurrentUtcTime()
    {
        var signer = new TokenSigner(privateKeyPem);
        var tokenString = signer.Sign("subject", "name", "issuer", "audience");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        var issuedAtClaim = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Iat).Value;
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(issuedAtClaim)).UtcDateTime;

        Assert.Equal(DateTime.UtcNow, issuedAt, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Sign_ShouldIncludeAdditionalClaims_WhenProvided()
    {
        var signer = new TokenSigner(privateKeyPem);
        const string claimType = "customClaim";
        const string claimValue = "customValue";
        var additionalClaims = new Dictionary<string, object>
        {
            { claimType, claimValue }
        };

        var tokenString = signer.Sign("subject", "name", "issuer", "audience", null, 120, additionalClaims);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal(claimValue, token.Claims.First(c => c.Type == claimType).Value);
    }
}

