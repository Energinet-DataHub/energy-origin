using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetFirstPartyOrganizationsTest : IntegrationTestBase
{
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetFirstPartyOrganizationsTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var connectionString = Fixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;

        _api = Fixture.WebAppFactory.CreateApi(sub: Fixture.WebAppFactory.AdminPortalEnterpriseAppRegistrationObjectId);
    }

    [Fact]
    public async Task GivenCallToEndpoint_WhenGettingFirstPartyOrganizations_ThenReturn200OKWithList()
    {
        var organization1 = Any.Organization(Any.Tin(), OrganizationName.Create("S/A Jahgilob Nairb"));
        var organization2 = Any.Organization(Any.Tin(), OrganizationName.Create("Brian Bolighaj A/S"));

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(organization1);
        await dbContext.Organizations.AddAsync(organization2);
        await dbContext.SaveChangesAsync();

        var response = await _api.GetFirstPartyOrganizations();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<FirstPartyOrganizationsResponse>();

        Assert.NotNull(content);
        Assert.IsType<FirstPartyOrganizationsResponse>(content);
        Assert.Contains(content!.Result, item =>
            item.OrganizationId == organization1.Id &&
            item.OrganizationName == organization1.Name.Value &&
            item.Tin == organization1.Tin!.Value
        );

        Assert.Contains(content.Result, item =>
            item.OrganizationId == organization2.Id &&
            item.OrganizationName == organization2.Name.Value &&
            item.Tin == organization2.Tin!.Value
        );
    }
}
