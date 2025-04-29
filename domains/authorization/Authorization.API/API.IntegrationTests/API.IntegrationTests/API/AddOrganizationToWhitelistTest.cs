using System.Net;
using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class AddOrganizationToWhitelistTest : IntegrationTestBase
{
    private readonly Api _api;
    private readonly ApplicationDbContext _db;

    public AddOrganizationToWhitelistTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _db = new ApplicationDbContext(options);

        _api = integrationTestFixture.WebAppFactory
            .CreateApi(sub: integrationTestFixture.WebAppFactory.AdminPortalEnterpriseAppRegistrationObjectId);
    }

    [Fact]
    public async Task Given_2_IdenticalCallsToEndpoint_When_AddingOrganization_Then_OnlyInsertOnce_AndReturnCreatedResponseBothTimes()
    {
        var tin = Any.Tin();

        var firstResponse = await _api.AddOrganizationToWhitelist(tin.Value);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var firstBody = await firstResponse.Content.ReadFromJsonAsync<AddOrganizationToWhitelistResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(firstBody);
        Assert.Equal(tin.Value, firstBody.Tin);

        var secondResponse = await _api.AddOrganizationToWhitelist(tin.Value);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        var secondBody = await secondResponse.Content.ReadFromJsonAsync<AddOrganizationToWhitelistResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(secondBody);
        Assert.Equal(tin.Value, secondBody.Tin);

        var count = await _db.Whitelisted
            .CountAsync(w => w.Tin == tin, TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }
}
