using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

public class GetOrganizationQueryTest : IntegrationTestBase, IClassFixture<IntegrationTestFixture>
{
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetOrganizationQueryTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var newDatabaseInfo = Fixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;
        _api = Fixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenOrganizationId_WhenGettingOrganization_OrganizationIsReturned()
    {
        // Given organization
        var organization = Any.Organization();
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.SaveChangesAsync();

        // When getting organization
        var response = await _api.GetOrganization(organization.Id);

        // Then
        response.Should().Be200Ok();
        var content = await response.Content.ReadFromJsonAsync<OrganizationResponse>();
        content!.OrganizationId.Should().Be(organization.Id);
        content.OrganizationName.Should().Be(organization.Name.Value);
    }

    [Fact]
    public async Task GivenUnknownOrganizationId_WhenGettingOrganization_404NotFound()
    {
        var response = await _api.GetOrganization(Guid.NewGuid());
        response.Should().Be404NotFound();
    }
}
