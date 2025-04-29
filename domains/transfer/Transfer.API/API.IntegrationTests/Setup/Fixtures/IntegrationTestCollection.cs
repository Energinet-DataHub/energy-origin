using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using EnergyTrackAndTrace.Testing.Testcontainers;
using NSubstitute;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using WireMock.Server;
using Xunit;

namespace API.IntegrationTests.Setup.Fixtures;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = "IntegrationTestCollection";
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public TransferAgreementsApiWebApplicationFactory Factory { get; private set; }
    public PostgresContainer PostgresContainer { get; private set; }
    public WireMockServer PdfGeneratorWireMock { get; private set; }
    public WireMockServer CvrWireMockServer { get; private set; }
    public RabbitMqContainer RabbitMqContainer { get; private set; }

    public IntegrationTestFixture()
    {
        Factory = new TransferAgreementsApiWebApplicationFactory();
        PostgresContainer = new PostgresContainer();
        RabbitMqContainer = new RabbitMqContainer();
        PdfGeneratorWireMock = WireMockServer.Start();
        CvrWireMockServer = WireMockServer.Start();
    }

    public async ValueTask InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();
        await RabbitMqContainer.InitializeAsync();

        SetupPoWalletClientMock();

        Factory.ConnectionString = PostgresContainer.ConnectionString;
        Factory.CvrBaseUrl = CvrWireMockServer.Url!;
        Factory.RabbitMqOptions = RabbitMqContainer.Options;
        Factory.PdfUrl = $"{PdfGeneratorWireMock.Url}/generate-pdf";
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

    public TestCaseContext CreateIsolatedWireMockTest([CallerMemberName] string testName = "")
    {
        return new TestCaseContext(Factory, PdfGeneratorWireMock, testName);
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}

