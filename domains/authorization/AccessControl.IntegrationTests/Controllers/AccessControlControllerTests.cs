using System;
using System.Net;
using AccessControl.IntegrationTests.Setup;
using FluentAssertions;
using Xunit;

namespace AccessControl.IntegrationTests.Controllers;

public class AccessControlControllerTests : EndToEndTestCase
{

    [Fact]
    public async Task Should_Reject_Unauthenticated_Requests()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/authorization/access-control");

        request.Headers.Add("EO_API_VERSION", "20230101");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // [Fact]
    // public async Task Decision_AuthenticatedWithValidOrgId_ReturnsOk()
    // {
    //     var response = await _api.Decision(organizationId);
    //
    //     response.StatusCode.Should().Be(HttpStatusCode.OK);
    // }
    //
    // [Fact]
    // public async Task Decision_Unauthenticated_ReturnsUnauthorized()
    // {
    //     var factory = new AccessControlWebApplicationFactory();
    //     var client = factory.CreateClient();
    //     var api = new Api(client);
    //
    //     var response = await api.Decision(Guid.NewGuid());
    //
    //     response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    // }
}
