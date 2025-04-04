using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetOrganizationQueryTest : IntegrationTestBase
{
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetOrganizationQueryTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenOrganizationId_WhenGettingOrganization_OrganizationIsReturned()
    {
        // Given organization
        var organization = Any.Organization();
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // When getting organization
        var response = await _api.GetOrganization(organization.Id);

        // Then
        response.Should().Be200Ok();
        var content = await response.Content.ReadFromJsonAsync<OrganizationResponse>(TestContext.Current.CancellationToken);
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
