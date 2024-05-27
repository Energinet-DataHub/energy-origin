using System.Net;
using FluentAssertions;
using Proxy.Controllers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Proxy.IntegrationTests;

public class ProxyIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private HttpClient Client => _fixture.Client ?? throw new InvalidOperationException("HttpClient is not initialized.");

    public ProxyIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Proxy_Forwards_Request_To_Downstream_With_Version_Header()
    {
        using var wireMockHelper = new WireMockServerHelper();

        wireMockHelper.Server
            .Given(Request.Create().WithPath("/v1/certificates").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));

        var request = new HttpRequestMessage(HttpMethod.Get, "/certificates");
        request.Headers.Add("EO_API_VERSION", ApiVersions.Version20250101);

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Proxy_Returns_Bad_Gateway_Without_Downstream()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/certificates");
        request.Headers.Add("EO_API_VERSION", ApiVersions.Version20250101);

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Proxy_Returns_Not_Found_For_Unmatched_Route()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/unknown/path");
        request.Headers.Add("EO_API_VERSION", ApiVersions.Version20250101);

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Proxy_Returns_BadRequest_If_Header_Missing()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/certificates");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Proxy_Returns_BadRequest_If_Invalid_Header_Version()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/certificates");
        request.Headers.Add("EO_API_VERSION", "13371337");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.MovedPermanently)]
    [InlineData(HttpStatusCode.Found)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Proxy_Forwards_Downstream_Response_Back_To_Client(HttpStatusCode statusCode)
    {
        using var wireMockHelper = new WireMockServerHelper();

        wireMockHelper.Server
            .Given(Request.Create().WithPath("/v1/certificates").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(statusCode));

        var request = new HttpRequestMessage(HttpMethod.Get, "/certificates");
        request.Headers.Add("EO_API_VERSION", "20250101");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task Proxy_Redirects_To_Downstream_Swagger_Endpoint()
    {
        using var wireMockHelper = new WireMockServerHelper();

        wireMockHelper.Server
            .Given(Request.Create().WithPath("/wallet-api-docs/20250101/swagger.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));

        var request = new HttpRequestMessage(HttpMethod.Get, "/wallet-api-docs/20250101/swagger.json");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
