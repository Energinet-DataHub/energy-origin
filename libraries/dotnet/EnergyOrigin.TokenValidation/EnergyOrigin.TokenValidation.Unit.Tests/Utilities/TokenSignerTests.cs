using EnergyOrigin.TokenValidation.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace EnergyOrigin.TokenValidation.Unit.Tests.Utilities;

public class TokenSignerTests
{
    private readonly byte[] _privateKeyPem;

    public TokenSignerTests()
    {
        var privateKeyString = RsaKeyGenerator.GenerateTestKey();
        _privateKeyPem = Encoding.UTF8.GetBytes(privateKeyString);
    }

    [Fact]
    public void Sign_ShouldGenerateToken_WithCorrectClaims()
    {
        var signer = new TokenSigner(_privateKeyPem);
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
        var signer = new TokenSigner(_privateKeyPem);
        var duration = 120;
        var issueAt = DateTime.UtcNow;

        var tokenString = signer.Sign("subject", "name", "issuer", "audience", issueAt, duration);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        var expectedExpiry = issueAt.AddSeconds(duration);
        Assert.True((expectedExpiry - token.ValidTo).TotalSeconds < 1);
    }

    [Fact]
    public void Sign_WithNullIssueAt_ShouldDefaultToCurrentUtcTime()
    {
        var signer = new TokenSigner(_privateKeyPem);
        var tokenString = signer.Sign("subject", "name", "issuer", "audience");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        var issuedAtClaim = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Iat).Value;
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(issuedAtClaim)).UtcDateTime;

        Assert.True((DateTime.UtcNow - issuedAt).TotalSeconds <= 5);
    }

    [Fact]
    public void Sign_ShouldIncludeAdditionalClaims_WhenProvided()
    {
        var signer = new TokenSigner(_privateKeyPem);

        var additionalClaims = new Dictionary<string, object>
        {
            { "customClaim", "customValue" }
        };

        var tokenString = signer.Sign("subject", "name", "issuer", "audience", null, 120, additionalClaims);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("customValue", token.Claims.First(c => c.Type == "customClaim").Value);
    }

    [Fact]
    public void Sign_WithNoAdditionalClaims_ShouldContainOnlyStandardClaims()
    {
        var signer = new TokenSigner(_privateKeyPem);

        var tokenString = signer.Sign("subject", "name", "issuer", "audience");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        var standardClaims = new[] { JwtRegisteredClaimNames.Sub, JwtRegisteredClaimNames.Name, JwtRegisteredClaimNames.Iss, JwtRegisteredClaimNames.Aud, JwtRegisteredClaimNames.Exp, JwtRegisteredClaimNames.Nbf, JwtRegisteredClaimNames.Iat };
        var tokenClaims = token.Claims.Select(c => c.Type).Distinct().ToArray();

        Assert.Equal(standardClaims.Length, tokenClaims.Length);
        Assert.All(tokenClaims, claimType => Assert.Contains(claimType, standardClaims));
    }
}

