using System.Net;
using AccessControl.IntegrationTests.Setup;
using FluentAssertions;

namespace AccessControl.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class AccessControlControllerTests
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;

    public AccessControlControllerTests(IntegrationTestFixture integrationTestFixture)
    {
        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task Decision_AuthenticatedWithValidOrgId_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();
        var api = _integrationTestFixture.WebAppFactory.CreateApi(orgIds: organizationId.ToString());

        var response = await api.Decision(organizationId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }


    [Fact]
    public async Task Decision_Unauthenticated_ReturnsUnauthorized()
    {
        var client = new HttpClient();
        var api = new Api(client);

        var response = await api.Decision(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
