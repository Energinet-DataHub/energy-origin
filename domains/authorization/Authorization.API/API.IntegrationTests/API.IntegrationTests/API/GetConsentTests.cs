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
public class GetConsentTests
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetConsentTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task AsUser_WhenGettingConsent_HttpOkConsentReturned()
    {
        var idpUserId = await SeedData();

        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: idpUserId.Value.ToString());

        var response = await userClient.GetUserOrganizationConsents();

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<GetUserOrganizationConsentsQueryResult>();
        result!.Result.Should().NotBeEmpty();
    }

    private async Task<IdpUserId> SeedData()
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
        return user.IdpUserId;
    }
}
