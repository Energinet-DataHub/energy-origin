using System.Net.Http.Json;
using API.Authorization._Features_;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
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
    public async Task GivenClientId_WhenGettingConsent_HttpOkConsentReturned()
    {
        var consent = Any.Consent();
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Consents.AddAsync(consent);


        await dbContext.SaveChangesAsync();
        var response = await _api.GetConsent(consent.ClientId);

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<GetConsentsQueryResult>();
        result!.Result.Should().NotBeEmpty();
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
