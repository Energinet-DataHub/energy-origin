using System.Net;
using FluentAssertions;
using Proxy.IntegrationTests.Setup;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Proxy.IntegrationTests;

public class V1EndpointTests(ProxyIntegrationTestFixture fixture) : IClassFixture<ProxyIntegrationTestFixture>
{
    private HttpClient CreateClientWithOrgIds(List<string> orgIds) => fixture.Factory.CreateAuthenticatedClient(orgIds: orgIds);
    private HttpClient CreateClientWithOrgAsSub(string sub) => fixture.Factory.CreateAuthenticatedClient(sub: sub);

    [Theory]
    [InlineData("GET", "/v1/wallets")]
    [InlineData("GET", "/v1/wallets/{walletId}")]
    [InlineData("GET", "/v1/certificates")]
    [InlineData("GET", "/v1/aggregate-certificates")]
    [InlineData("GET", "/v1/claims")]
    [InlineData("GET", "/v1/aggregate-claims")]
    [InlineData("GET", "/v1/transfers")]
    [InlineData("GET", "/v1/aggregate-transfers")]
    public async Task V1_Endpoints_ReturnOk(string method, string v1ProxyEndpoint)
    {

        var endpoint = v1ProxyEndpoint.Contains("{walletId}") ? v1ProxyEndpoint.Replace("{walletId}", Guid.NewGuid().ToString()) : v1ProxyEndpoint;

        var requestBuilder = Request.Create().WithPath(endpoint).UsingMethod(method);

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(Response.Create().WithStatusCode(200));

        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var client = CreateClientWithOrgIds(orgIds);

        var queryParameters = "";
        if (v1ProxyEndpoint.StartsWith("/v1/aggregate-"))
        {
            queryParameters = "?TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600";
        }

        var request = new HttpRequestMessage(new HttpMethod(method), $"{endpoint}{queryParameters}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("GET", "/wallets")]
    [InlineData("GET", "/wallets/{walletId}")]
    [InlineData("GET", "/certificates")]
    [InlineData("GET", "/aggregate-certificates")]
    [InlineData("GET", "/claims")]
    [InlineData("GET", "/aggregate-claims")]
    [InlineData("GET", "/transfers")]
    [InlineData("GET", "/aggregate-transfers")]
    public async Task V20250101_Endpoints_ReturnOk(string method, string v2025ProxyEndpoint)
    {
        var walletId = Guid.NewGuid().ToString();
        var downstreamEndpoint = v2025ProxyEndpoint.Contains("{walletId}") ? v2025ProxyEndpoint.Replace("{walletId}", walletId) : v2025ProxyEndpoint;

        var requestBuilder = Request.Create()
            .WithPath($"/v1{downstreamEndpoint}")
            .UsingMethod(method);

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(Response.Create().WithStatusCode(200));

        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var client = CreateClientWithOrgIds(orgIds);
        var organizationId = orgIds[0];

        var queryParameters = $"?organizationId={organizationId}";
        if (v2025ProxyEndpoint.StartsWith("/aggregate-"))
        {
            queryParameters += "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600";
        }

        var request = new HttpRequestMessage(new HttpMethod(method), $"{downstreamEndpoint}{queryParameters}");
        request.Headers.Add("EO_API_VERSION", "20250101");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("GET", "/v1/wallets")]
    [InlineData("GET", "/v1/wallets/{walletId}")]
    [InlineData("GET", "/v1/certificates")]
    [InlineData("GET", "/v1/aggregate-certificates")]
    [InlineData("GET", "/v1/claims")]
    [InlineData("GET", "/v1/aggregate-claims")]
    [InlineData("GET", "/v1/transfers")]
    [InlineData("GET", "/v1/aggregate-transfers")]
    public async Task GivenOldAuth_WhenV1EndpointsAreUsed_ThenAppendSubClaimAsWalletOwnerHeader(string method, string v1ProxyEndpoint)
    {
        var endpoint = v1ProxyEndpoint.Contains("{walletId}") ? v1ProxyEndpoint.Replace("{walletId}", Guid.NewGuid().ToString()) : v1ProxyEndpoint;

        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var organizationId = orgIds[0];

        var requestBuilder = Request.Create().WithPath(endpoint).UsingMethod(method);

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("{{request.headers.wallet-owner}}")
                    .WithTransformer()
            );

        var client = CreateClientWithOrgAsSub(sub: organizationId);

        var queryParameters = "";
        if (v1ProxyEndpoint.StartsWith("/v1/aggregate-"))
        {
            queryParameters = "?TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600";
        }

        var request = new HttpRequestMessage(new HttpMethod(method), $"{endpoint}{queryParameters}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(organizationId);
    }

    [Theory]
    [InlineData("GET", "/wallets")]
    [InlineData("GET", "/wallets/{walletId}")]
    [InlineData("GET", "/certificates")]
    [InlineData("GET", "/aggregate-certificates")]
    [InlineData("GET", "/claims")]
    [InlineData("GET", "/aggregate-claims")]
    [InlineData("GET", "/transfers")]
    [InlineData("GET", "/aggregate-transfers")]
    public async Task GivenB2C_WhenV20250101EndpointsAreUsed_ThenAppendQueryParameterAsWalletOwnerHeader(string method, string v2025ProxyEndpoint)
    {
        var walletId = Guid.NewGuid().ToString();
        var downstreamEndpoint = v2025ProxyEndpoint.Contains("{walletId}") ? v2025ProxyEndpoint.Replace("{walletId}", walletId) : v2025ProxyEndpoint;

        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var organizationId = orgIds[0];

        var requestBuilder = Request.Create()
            .WithPath($"/v1{downstreamEndpoint}")
            .UsingMethod(method);

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("{{request.headers.wallet-owner}}")
                    .WithTransformer()
            );

        var client = CreateClientWithOrgIds(orgIds);

        var queryParameters = $"?organizationId={organizationId}";
        if (v2025ProxyEndpoint.StartsWith("/aggregate-"))
        {
            queryParameters += "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600";
        }

        var request = new HttpRequestMessage(new HttpMethod(method), $"{downstreamEndpoint}{queryParameters}");
        request.Headers.Add("EO_API_VERSION", "20250101");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(organizationId);
    }
}
