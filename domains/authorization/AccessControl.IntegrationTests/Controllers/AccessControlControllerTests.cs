using System;
using System.Net;
using AccessControl.IntegrationTests.Setup;
using FluentAssertions;
using Xunit;

namespace AccessControl.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class AccessControlControllerTests : IClassFixture<AccessControlWebApplicationFactory>
{
    private readonly Api _api;
    private static readonly Guid organizationId = new("0eeec713-df51-442d-8550-02e0a4301c9d");

    public AccessControlControllerTests(AccessControlWebApplicationFactory factory)
    {
        _api = factory.CreateApi();
    }

    [Fact]
    public async Task Decision_AuthenticatedWithValidOrgId_ReturnsOk()
    {
        var response = await _api.Decision(organizationId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Decision_Unauthenticated_ReturnsUnauthorized()
    {
        var factory = new AccessControlWebApplicationFactory();
        var client = factory.CreateClient();
        var api = new Api(client);

        var response = await api.Decision(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
