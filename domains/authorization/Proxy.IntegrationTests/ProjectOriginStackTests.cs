using System.Net;
using System.Text;
using FluentAssertions;
using Proxy.IntegrationTests.Testcontainers;

namespace Proxy.IntegrationTests;

public class ProjectOriginStackTests : IAsyncLifetime
{
    private readonly ProjectOriginStack _projectOriginStack = new();

    [Fact]
    public async Task Confirm_WalletSetup_returns401_ifNoHeader()
    {
        var walletUrl = _projectOriginStack.WalletUrl;
        var certificatesEndpoint = $"{walletUrl}v1/certificates";

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(certificatesEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Confirm_WalletSetup_returns201_IfReceivingHeader()
    {
        var walletUrl = _projectOriginStack.WalletUrl;
        var walletEndpoint = $"{walletUrl}v1/wallets";
        var organizationId = Guid.NewGuid().ToString();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("wallet-owner", organizationId);
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(walletEndpoint, content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public async Task InitializeAsync()
    {
        await _projectOriginStack.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _projectOriginStack.DisposeAsync();
    }
}
