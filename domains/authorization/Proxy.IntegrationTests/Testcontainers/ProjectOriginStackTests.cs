using System.Net;
using System.Text;
using FluentAssertions;

namespace Proxy.IntegrationTests.Testcontainers;
/*
public class ProjectOriginStackTests(ProjectOriginStackFixture fixture) : IClassFixture<ProjectOriginStackFixture>
{
    private readonly ProjectOriginStack _projectOriginStack = fixture.ProjectOriginStack;

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
}
*/
