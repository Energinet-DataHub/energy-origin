using System.Net.Http.Json;
using API.Authorization._Features_;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GrantConsentTest
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> options;

    public GrantConsentTest(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.ConnectionString;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.CreateApi();
    }

    [Fact]
    public async Task GivenUnknownClientId_WhenGrantingConsent_HttpNotFoundIsReturned()
    {
        var unknownClientId = Guid.NewGuid();
        var response = await _api.GrantConsent(unknownClientId);
        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GivenKnownClientId_WhenGrantingConsent_200OkReturned()
    {
        var client = Any.Client();
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Clients.AddAsync(client);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.SaveChangesAsync();

        var api = _integrationTestFixture.CreateApi(sub: user.Id.ToString(), orgIds: organization.Id.ToString());
        var response = await api.GrantConsent(client.IdpClientId.Value);
        response.Should().Be200Ok();
    }

    [Fact]
    public async Task GivenClientId_WhenGettingConsent_HttpOkConsentReturned()
    {
        var idpClientId = await SeedDataAndReturnIdpClientId();

        var response = await _api.GetConsent(idpClientId.Value);

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<GetConsentsQueryResult>();
        result!.Result.Should().NotBeEmpty();
    }

    private async Task<IdpClientId> SeedDataAndReturnIdpClientId()
    {
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);
        var client = Any.Client();
        var consent = Consent.Create(organization, client, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.Clients.AddAsync(client);
        await dbContext.Consents.AddAsync(consent);

        await dbContext.SaveChangesAsync();
        return client.IdpClientId;
    }

    [Fact]
    public async Task GivenSubTypeNotUser_WhenGrantingConsent_HttpForbiddenIsReturned()
    {
        var api = _integrationTestFixture.CreateApi(subType: "external");

        var response = await api.GrantConsent(Guid.NewGuid());

        response.Should().Be403Forbidden();
    }

    [Fact]
    public async Task GivenSubTypeNotUser_WhenGettingConsent_HttpForbiddenIsReturned()
    {
        var api = _integrationTestFixture.CreateApi(subType: "external");

        var response = await api.GetConsent(Guid.NewGuid());

        response.Should().Be403Forbidden();
    }
}
