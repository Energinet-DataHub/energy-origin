using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.UnitTests;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using EnergyTrackAndTrace.Testing.Extensions;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Meteringpoint.V1;
using NSubstitute;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using WireMock.Server;
using Xunit;
using static Meteringpoint.V1.Meteringpoint;

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
        SetupMeteringpointClientMock();

        Factory.ConnectionString = PostgresContainer.ConnectionString;
        Factory.CvrBaseUrl = CvrWireMockServer.Url!;
        Factory.RabbitMqOptions = RabbitMqContainer.Options;
        Factory.PdfUrl = $"{PdfGeneratorWireMock.Url}/generate-pdf";
        Factory.Start();
    }

    private IWalletClient SetupPoWalletClientMock()
    {
        var walletClientMock = Factory.WalletClientMock;
        walletClientMock.CreateWalletAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        walletClientMock.GetWalletsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(
            new ResultList<WalletRecord>
            {
                Metadata = new PageInfo { Count = 1, Limit = 100, Total = 1, Offset = 0 },
                Result = new List<WalletRecord>
                {
                    new WalletRecord
                        { Id = Guid.NewGuid(), PublicKey = new Secp256k1Algorithm().GenerateNewPrivateKey().Neuter(), DisabledDate = null }
                }
            });
        walletClientMock.CreateWalletEndpointAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(
            new WalletEndpointReference(1, new Uri("http://someUrl"),
                new Secp256k1Algorithm().GenerateNewPrivateKey().Neuter()));
        walletClientMock
            .CreateExternalEndpointAsync(Arg.Any<Guid>(), Arg.Any<WalletEndpointReference>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new CreateExternalEndpointResponse { ReceiverId = Guid.NewGuid() });

        return walletClientMock;
    }

    private void SetupMeteringpointClientMock()
    {
        var gsrn = Any.Gsrn();

        var meteringpointClientMock = Factory.MeteringpointClientMock;
        meteringpointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn)
                }
            });
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

