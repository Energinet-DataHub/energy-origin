using System.Net;
using EnergyOrigin.Setup;
using FluentAssertions;
using Proxy.IntegrationTests.Setup;

namespace Proxy.IntegrationTests;

public class ErrorHandlingIntegrationTests(ProxyIntegrationTestFixture fixture) : IClassFixture<ProxyIntegrationTestFixture>
{
    private static string Endpoint => "/wallet-api/certificates";
    private HttpClient CreateClientWithOrgIds(Guid orgId, List<string> orgIds)
        => fixture.Factory.CreateAuthenticatedClient(orgId: orgId.ToString(), orgIds: orgIds);

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task Given_MalformedOrgId_Returns400_And_IsNotForwarded(string orgIdParam)
    {
        var orgId = Guid.NewGuid();
        var authorised = new List<string> { orgId.ToString() };
        var client = CreateClientWithOrgIds(orgId, authorised);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoint}?organizationId={orgIdParam}");
        request.Headers.Add("X-API-Version", ApiVersions.Version1);

        var response = await client.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        fixture.WalletWireMockServer.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_Unknown_Or_UnauthorisedOrgId_Returns403_And_IsNotForwarded()
    {
        var callerOrg = Guid.NewGuid();
        var authorised = new List<string> { callerOrg.ToString() };
        var client = CreateClientWithOrgIds(callerOrg, authorised);

        var unknownOrg = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoint}?organizationId={unknownOrg}");
        request.Headers.Add("X-API-Version", ApiVersions.Version1);

        var response = await client.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        fixture.WalletWireMockServer.LogEntries.Should().BeEmpty();
    }
}
