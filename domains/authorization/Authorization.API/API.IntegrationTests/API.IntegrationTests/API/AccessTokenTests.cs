using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class AccessTokenTests
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AccessTokenTests(IntegrationTestFixture integrationTestFixture)
    {
        _integrationTestFixture = integrationTestFixture;
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenConsent_WhenDeleted_ThenAccessTokenShouldNotContainOrganizationId()
    {
        // Step 1: Seed database with a user, an organization, and a client
        var user = Any.User();
        var organization = Any.Organization();
        var client = Client.Create(new IdpClientId(Guid.NewGuid()), new("Loz"), ClientType.Internal, "https://localhost:5001");
        var consent = Consent.Create(organization, client, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Clients.AddAsync(client);
        await dbContext.Consents.AddAsync(consent);
        var affiliation = Affiliation.Create(user, organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.SaveChangesAsync();

        // Step 2: Generate and validate access token before deletion
        var accessTokenBefore = GenerateToken(client.IdpClientId.Value.ToString(), user.Name.Value, organization.Id.ToString(), "user", organization.Tin.Value);
        var orgIdsBefore = GetOrgIdsFromAccessToken(accessTokenBefore);
        orgIdsBefore.Should().Contain(organization.Id.ToString());

        // Step 3: Delete the consent
        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin.Value);
        var deleteResponse = await userClient.DeleteConsent(client.Id, organization.Id);
        deleteResponse.Should().Be204NoContent();

        // Step 4: Generate and validate access token after deletion
        var accessTokenAfter = GenerateToken(client.IdpClientId.Value.ToString(), user.Name.Value, "", "user", organization.Tin.Value);
        var orgIdsAfter = GetOrgIdsFromAccessToken(accessTokenAfter);
        orgIdsAfter.Should().NotContain(organization.Id.ToString());
    }

    private string GenerateToken(string sub, string name, string orgIds, string subType, string orgCvr)
    {
        using RSA rsa = RSA.Create(2048);
        var req = new CertificateRequest("cn=eotest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        var signingCredentials = new SigningCredentials(new X509SecurityKey(cert), SecurityAlgorithms.RsaSha256);
        var tokenHandler = new JwtSecurityTokenHandler();
        var identity = new ClaimsIdentity(new List<Claim>
        {
            new("sub", sub),
            new("name", name),
            new("org_ids", orgIds),
            new("sub_type", subType),
            new("org_cvr", orgCvr)
        });
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = "audience",
            Issuer = "issuer",
            NotBefore = DateTime.Now.AddHours(-1),
            Expires = DateTime.Now.AddHours(1),
            SigningCredentials = signingCredentials,
            Subject = identity
        };

        var token = tokenHandler.CreateToken(securityTokenDescriptor);
        var encodedAccessToken = tokenHandler.WriteToken(token);
        return encodedAccessToken!;
    }

    private IEnumerable<string> GetOrgIdsFromAccessToken(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(accessToken);
        var orgIdsClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "org_ids")?.Value;
        return orgIdsClaim?.Split(',') ?? Enumerable.Empty<string>();
    }
}
