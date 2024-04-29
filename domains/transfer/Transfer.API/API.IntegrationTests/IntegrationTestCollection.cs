using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Testcontainers;
using API.Transfer.Api.Services;
using NSubstitute;
using NSubstitute.ClearExtensions;
using WireMock.Server;
using Xunit;

namespace API.IntegrationTests;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = "IntegrationTestCollection";
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public TransferAgreementsApiWebApplicationFactory Factory { get; private set; }
    public PostgresContainer PostgresContainer { get; private set; }
    public WireMockServer CvrWireMockServer { get; private set; }

    public IntegrationTestFixture()
    {
        Factory = new TransferAgreementsApiWebApplicationFactory();
        PostgresContainer = new PostgresContainer();
        CvrWireMockServer = WireMockServer.Start();
    }

    public async Task InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();

        SetupWalletServiceMock();

        Factory.ConnectionString = PostgresContainer.ConnectionString;
        Factory.CvrBaseUrl = CvrWireMockServer.Url!;
        Factory.Start();
    }

    private void SetupWalletServiceMock()
    {
        var poWalletServiceMock = Factory.WalletServiceMock;
        poWalletServiceMock.ClearSubstitute();
        poWalletServiceMock.CreateWalletDepositEndpoint(Arg.Any<AuthenticationHeaderValue>()).Returns("SomeToken");
        poWalletServiceMock.CreateReceiverDepositEndpoint(Arg.Any<AuthenticationHeaderValue>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Guid.NewGuid());
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}

