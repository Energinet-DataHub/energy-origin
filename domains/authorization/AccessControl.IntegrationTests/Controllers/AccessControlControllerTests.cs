using System.Net;
using AccessControl.IntegrationTests.Setup;
using FluentAssertions;
using Xunit;

namespace AccessControl.IntegrationTests.Controllers;

public class AccessControlControllerTests(AccessControlWebApplicationFactory factory)
    : IClassFixture<AccessControlWebApplicationFactory>
{
    [Fact]
    public async Task Decision_AuthenticatedWithValidOrgId_ReturnsOk()
    {
        var orgId = "b63c357f-1732-4016-ba28-a9066ff9f03c";
        var client = factory.CreateAuthenticatedClient(sub: orgId);

        var response = await client.GetAsync($"/api/decision?organizationId={orgId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
