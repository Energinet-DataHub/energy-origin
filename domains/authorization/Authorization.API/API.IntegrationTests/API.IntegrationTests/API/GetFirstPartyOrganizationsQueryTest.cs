using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetFirstPartyOrganizationsTest
{
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetFirstPartyOrganizationsTest(IntegrationTestFixture integrationTestFixture)
    {
        _integrationTestFixture = integrationTestFixture;
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;

        _api = integrationTestFixture.WebAppFactory.CreateApi(
            sub: _integrationTestFixture.WebAppFactory.IssuerIdpClientId.ToString()
        );
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

        response.Should().Be200Ok();

        var content = await response.Content.ReadFromJsonAsync<FirstPartyOrganizationsResponse>();

        content.Should().NotBeNull();
        content!.Result.Should().ContainEquivalentOf(new FirstPartyOrganizationsResponseItem(
            organization1.Id,
            organization1.Name.Value,
            organization1.Tin!.Value
        ));

        content.Result.Should().ContainEquivalentOf(new FirstPartyOrganizationsResponseItem(
            organization2.Id,
            organization2.Name.Value,
            organization2.Tin!.Value
        ));
    }
}
