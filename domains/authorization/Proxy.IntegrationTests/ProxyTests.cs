using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Proxy.IntegrationTests;

public class ProxyTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Proxy_Should_Append_Header_And_Forward_Request()
    {
        var client = factory.CreateAuthenticatedClient(orgIds: new List<string> { "test_org" });
        var organizationId = "test_org";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/resource?organizationId={organizationId}");

        var wireMockServer = WireMockServer.Start(port: 5001);
        wireMockServer
            .Given(Request.Create().WithPath("/api/resource").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("{{request.headers.wallet-owner}}")
                    .WithTransformer()
            );

        client.BaseAddress = new Uri(wireMockServer.Url!);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.Equal(organizationId, responseContent);

        wireMockServer.Stop();
    }
}
