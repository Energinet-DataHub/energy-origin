using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers.Internal;

[Collection(IntegrationTestCollection.CollectionName)]
public class AdminPortalControllerTests : IntegrationTestBase
{
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AdminPortalControllerTests(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;
        _api = integrationTestFixture.WebAppFactory
            .CreateApi(sub: integrationTestFixture.WebAppFactory.AdminPortalEnterpriseAppRegistrationObjectId);
    }

    [Fact]
    public async Task GetOrganization_ReturnsOrganization()
    {
        await using var dbContext = new ApplicationDbContext(_options);

        var orgDb = Organization.CreateTrial(EnergyTrackAndTrace.Testing.Any.Tin(), Any.OrganizationName());
        dbContext.Organizations.Add(orgDb);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await _api.GetOrganizationForAdminPortal(orgDb.Id);

        var result = await response.Content.ReadFromJsonAsync<AdminPortalOrganizationResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(orgDb.Name.Value, result.OrganizationName);
        Assert.Equal(orgDb.Tin?.Value, result.Tin);
        Assert.Equal(orgDb.Id, result.OrganizationId);
        Assert.Equal(Authorization.Controllers.OrganizationStatus.Trial, result.Status);
    }
}
