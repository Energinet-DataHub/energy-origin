using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.UnitTests;
using FluentAssertions;

namespace API.IntegrationTests.API;

public class GetOrganizationQueryTest : IntegrationTestBase, IAsyncLifetime
{
    private readonly Api _api;

    public GetOrganizationQueryTest(IntegrationTestFixture fixture) : base(fixture)
    {
        _api = _fixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenOrganizationId_WhenGettingOrganization_OrganizationIsReturned()
    {
        // Given organization
        var organization = Any.Organization();
        await _fixture.DbContext.Organizations.AddAsync(organization);
        await _fixture.DbContext.SaveChangesAsync();

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
