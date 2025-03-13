using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using EnergyTrackAndTrace.Testing.Testcontainers;
using NSubstitute;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
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
    public PostgresDatabase PostgresDatabase { get; private set; }
    public WireMockServer CvrWireMockServer { get; private set; }

    public IntegrationTestFixture()
    {
        Factory = new TransferAgreementsApiWebApplicationFactory();
        PostgresDatabase = new PostgresDatabase();
        CvrWireMockServer = WireMockServer.Start();
    }

    public async Task InitializeAsync()
    {
        await PostgresDatabase.InitializeAsync();

        SetupPoWalletClientMock();

        Factory.ConnectionString = (await PostgresDatabase.CreateNewDatabase()).ConnectionString;
        Factory.CvrBaseUrl = CvrWireMockServer.Url!;
        Factory.Start();
    }

    private IWalletClient SetupPoWalletClientMock()
    {
        var walletClientMock = Factory.WalletClientMock;
        walletClientMock.CreateWallet(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        walletClientMock.GetWallets(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(
            new ResultList<WalletRecord>
            {
                Metadata = new PageInfo { Count = 1, Limit = 100, Total = 1, Offset = 0 },
                Result = new List<WalletRecord>
                {
                    new WalletRecord
                        { Id = Guid.NewGuid(), PublicKey = new Secp256k1Algorithm().GenerateNewPrivateKey().Neuter() }
                }
            });
        walletClientMock.CreateWalletEndpoint(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(
            new WalletEndpointReference(1, new Uri("http://someUrl"),
                new Secp256k1Algorithm().GenerateNewPrivateKey().Neuter()));
        walletClientMock
            .CreateExternalEndpoint(Arg.Any<Guid>(), Arg.Any<WalletEndpointReference>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new CreateExternalEndpointResponse { ReceiverId = Guid.NewGuid() });

        return walletClientMock;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}

