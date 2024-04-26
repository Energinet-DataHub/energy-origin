using ProjectOriginClientsTests.Testcontainers;
using Xunit;
using FluentAssertions;
using ProjectOriginClients;

namespace ProjectOriginClientsTests;

public class ProjectOriginWalletClientTests : IClassFixture<ProjectOriginStack>
{
    private readonly ProjectOriginStack poStack;

    public ProjectOriginWalletClientTests(ProjectOriginStack poStack)
    {
        this.poStack = poStack;
    }

    [Fact]
    public async Task CreateAndGetWallets()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient);

        var createWalletResponse = await walletClient.CreateWallet(ownerSubject, new CancellationToken());

        createWalletResponse.Should().NotBeNull();

        var wallets = await walletClient.GetWallets(ownerSubject, new CancellationToken());

        wallets.Should().NotBeNull();
        wallets.Result.Count().Should().Be(1);
    }

    [Fact]
    public async Task CreateWalletEndpoint()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient);

        var createWalletResponse = await walletClient.CreateWallet(ownerSubject, new CancellationToken());

        createWalletResponse.Should().NotBeNull();

        var walletEndpoint = await walletClient.CreateWalletEndpoint(ownerSubject, createWalletResponse.WalletId, new CancellationToken());

        walletEndpoint.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateExternalEndpoint()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient);

        var createWalletResponse = await walletClient.CreateWallet(ownerSubject, new CancellationToken());

        createWalletResponse.Should().NotBeNull();

        var walletEndpoint = await walletClient.CreateWalletEndpoint(ownerSubject, createWalletResponse.WalletId, new CancellationToken());

        walletEndpoint.Should().NotBeNull();
        var cvrNumber = "12345678";
        var externalEndpoint = await walletClient.CreateExternalEndpoint(Guid.NewGuid(), walletEndpoint, cvrNumber, new CancellationToken());

        externalEndpoint.Should().NotBeNull();
        externalEndpoint.ReceiverId.Should().NotBeEmpty();
    }

    private HttpClient GetWalletHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(poStack.WalletUrl);

        return client;
    }
}
