using System.Net;
using FluentAssertions;
using Proxy.Controllers;
using Proxy.IntegrationTests.Setup;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Proxy.IntegrationTests;

public class ProxyBaseIntegrationTests(ProxyIntegrationTestFixture fixture) : IClassFixture<ProxyIntegrationTestFixture>
{
    private HttpClient CreateClientWithOrgIds(List<string> orgIds)
    {
        return fixture.Factory.CreateAuthenticatedClient(orgIds: orgIds);
    }

    [Fact]
    public async Task Proxy_Forwards_Request_To_Downstream_With_Version_Header()
    {

        fixture.WalletWireMockServer
            .Given(Request.Create().WithPath("/wallet-api/v1/certificates").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));

        var orgIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
        var client = CreateClientWithOrgIds(orgIds);
        var organizationId = orgIds[0];

        var request = new HttpRequestMessage(HttpMethod.Get, $"/wallet-api/certificates?organizationId={organizationId}");
        request.Headers.Add("EO_API_VERSION", ApiVersions.Version20240515);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Proxy_Returns_Not_Found_For_Unmatched_Route()
    {
        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var client = CreateClientWithOrgIds(orgIds);
        var organizationId = orgIds[0];

        var request = new HttpRequestMessage(HttpMethod.Get, $"/unknown/path?organizationId={organizationId}");
        request.Headers.Add("EO_API_VERSION", ApiVersions.Version20240515);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Proxy_Returns_BadRequest_If_Header_Missing()
    {
        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var client = CreateClientWithOrgIds(orgIds);
        var organizationId = orgIds[0];

        var request = new HttpRequestMessage(HttpMethod.Get, $"/wallet-api/certificates?organizationId={organizationId}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Proxy_Returns_BadRequest_If_Invalid_Header_Version()
    {
        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var client = CreateClientWithOrgIds(orgIds);
        var organizationId = orgIds[0];

        var request = new HttpRequestMessage(HttpMethod.Get, $"/wallet-api/certificates?organizationId={organizationId}");
        request.Headers.Add("EO_API_VERSION", "13371337");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
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

        fixture.WalletWireMockServer
            .Given(Request.Create().WithPath("/wallet-api/v1/certificates").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(statusCode));

        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var client = CreateClientWithOrgIds(orgIds);
        var organizationId = orgIds[0];

        var request = new HttpRequestMessage(HttpMethod.Get, $"/wallet-api/certificates?organizationId={organizationId}");
        request.Headers.Add("EO_API_VERSION", ApiVersions.Version20240515);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(statusCode);
    }
}
