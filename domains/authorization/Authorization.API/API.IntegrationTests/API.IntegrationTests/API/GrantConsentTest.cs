using System.Net.Http.Json;
using API.Authorization._Features_;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using EnergyOrigin.TokenValidation.b2c;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GrantConsentTest
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GrantConsentTest(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
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
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Clients.AddAsync(client);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.SaveChangesAsync();

        var api = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin.Value);
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

        await using var dbContext = new ApplicationDbContext(_options);
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
        var api = _integrationTestFixture.WebAppFactory.CreateApi(subType: SubjectType.External.ToString(), termsAccepted: false);

        var response = await api.GrantConsent(Guid.NewGuid());

        response.Should().Be403Forbidden();
    }

    [Fact]
    public async Task GivenSubTypeNotUser_WhenGettingConsent_HttpForbiddenIsReturned()
    {
        var api = _integrationTestFixture.WebAppFactory.CreateApi(subType: SubjectType.External.ToString(), termsAccepted: false);

        var response = await api.GetConsent(Guid.NewGuid());

        response.Should().Be403Forbidden();
    }
}
