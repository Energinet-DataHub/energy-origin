using System.Net;
using FluentAssertions;
using Proxy.IntegrationTests.Fixtures;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Proxy.IntegrationTests;

public class OcelotIntegrationTests(IntegrationTestFixture integrationFixture) : IClassFixture<IntegrationTestFixture>
{
    private HttpClient Client => integrationFixture.Client ?? throw new InvalidOperationException("HttpClient is not initialized.");

    [Fact]
    public async Task Ocelot_Forwards_Request_To_Downstream_With_Version_Header()
    {
        using var wireMockHelper = new WireMockServerHelper();

        wireMockHelper.Server
            .Given(Request.Create().WithPath("/v1/test").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
        request.Headers.Add("EO_API_VERSION", "20250101");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ocelot_Returns_Bad_Gateway_Without_WireMock()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
        request.Headers.Add("EO_API_VERSION", "20250101");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Ocelot_Returns_Not_Found_For_Unmatched_Route()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/unknown/path");
        request.Headers.Add("EO_API_VERSION", "20250101");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
