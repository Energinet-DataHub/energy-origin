using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProjectOriginClientsTests.Testcontainers;
using Xunit;

namespace ProjectOriginClients.Tests;

public class ProjectOriginWalletClientTests : IClassFixture<ProjectOriginStack>
{
    private readonly ProjectOriginStack poStack;
    private readonly ILogger<ProjectOriginWalletClient> logger;

    public ProjectOriginWalletClientTests(ProjectOriginStack poStack)
    {
        this.poStack = poStack;
        this.logger = Substitute.For<ILogger<ProjectOriginWalletClient>>();
    }

    [Fact]
    public async Task CreateAndGetWallets()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, logger);

        var createWalletResponse = await walletClient.CreateWallet(ownerSubject, new CancellationToken());

        createWalletResponse.Should().NotBeNull();

        var wallets = await walletClient.GetWallets(ownerSubject, new CancellationToken());

        wallets.Should().NotBeNull();
        wallets.Result.Count().Should().Be(1);
    }

    [Fact]
    public async Task GetWallets_WhenNoWallets_ExpectEmptyResult()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, logger);

        var wallets = await walletClient.GetWallets(ownerSubject, new CancellationToken());

        wallets.Should().NotBeNull();
        wallets.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateWalletEndpoint()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, logger);

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
        var walletClient = new ProjectOriginWalletClient(httpClient, logger);

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
