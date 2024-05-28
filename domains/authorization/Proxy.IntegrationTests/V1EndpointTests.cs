using System.Net;
using System.Text;
using FluentAssertions;
using Proxy.Controllers;
using Proxy.IntegrationTests.Setup;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Proxy.IntegrationTests;

public class V1EndpointTests(ProxyIntegrationTestFixture fixture) : IClassFixture<ProxyIntegrationTestFixture>
{
    private HttpClient CreateClientWithOrgIds(List<string> orgIds)
    {
        return fixture.Factory.CreateAuthenticatedClient(orgIds: orgIds);
    }

    [Theory]
    [InlineData("GET", "/v1/wallets")]
    [InlineData("POST", "/v1/wallets")]
    [InlineData("GET", "/v1/wallets/{walletId}")]
    [InlineData("POST", "/v1/wallets/{walletId}/endpoints")]
    [InlineData("POST", "/v1/external-endpoints")]
    [InlineData("GET", "/v1/certificates")]
    [InlineData("GET", "/v1/aggregate-certificates")]
    [InlineData("GET", "/v1/claims")]
    [InlineData("POST", "/v1/claims")]
    [InlineData("GET", "/v1/aggregate-claims")]
    [InlineData("POST", "/v1/slices")]
    [InlineData("GET", "/v1/transfers")]
    [InlineData("POST", "/v1/transfers")]
    [InlineData("GET", "/v1/aggregate-transfers")]
    public async Task Test_Endpoints_ReturnOk(string method, string v1Endpoint)
    {
        using var wireMockHelper = new ProxyWireMockServerHelper();

        var endpoint = v1Endpoint.Contains("{walletId}") ? v1Endpoint.Replace("{walletId}", Guid.NewGuid().ToString()) : v1Endpoint;

        var requestBuilder = Request.Create().WithPath(endpoint).UsingMethod(method);
        if (method == "POST")
        {
            requestBuilder = requestBuilder.WithBody(new Func<string, bool>(body => true));
        }

        wireMockHelper.Server
            .Given(requestBuilder)
            .RespondWith(Response.Create().WithStatusCode(200));

        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var client = CreateClientWithOrgIds(orgIds);
        var organizationId = orgIds[0];

        var queryParameters = "";
        if (v1Endpoint.StartsWith("/v1/aggregate-"))
        {
            queryParameters = "?TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600";
        }
        else
        {
            queryParameters = $"?organizationId={organizationId}";
        }

        var request = new HttpRequestMessage(new HttpMethod(method), $"{endpoint}{queryParameters}");
        request.Headers.Add("EO_API_VERSION", "1");

        if (method == "POST")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}



