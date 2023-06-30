extern alias registryConnector;
using System;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using FluentAssertions;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.PedersenCommitment;
using registryConnector::ProjectOrigin.Electricity.V1;
using registryConnector::RegistryConnector.Worker;
using Testcontainers.PostgreSql;
using Xunit;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace API.IntegrationTests;

public class HoleTest : IClassFixture<RegistryConnectorApplicationFactory>, IClassFixture<WalletContainer>
{
    private readonly RegistryConnectorApplicationFactory factory;
    private readonly WalletContainer walletContainer;

    public HoleTest(RegistryConnectorApplicationFactory factory, WalletContainer walletContainer)
    {
        this.factory = factory;
        this.walletContainer = walletContainer;

        factory.ProjectOriginOptions = new ProjectOriginOptions
        {
            WalletUrl = walletContainer.WalletUrl,
        };
    }

    [Fact]
    public async Task Test1()
    {
        factory.Start();

        await Task.Delay(TimeSpan.FromSeconds(10));

        walletContainer.WalletUrl.Should().Be("http://127.0.0.1:7890/");
    }
}

public class HoleTest2 : IClassFixture<RegistryFixture>
{
    private readonly RegistryFixture registryFixture;

    public HoleTest2(RegistryFixture registryFixture)
    {
        this.registryFixture = registryFixture;
    }

    [Fact]
    public async Task Test1()
    {
        var ownerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var secretCommitmentInfo = new SecretCommitmentInfo(250);
        var issuedCert = await registryFixture.IssueCertificate(GranularCertificateType.Production, secretCommitmentInfo,ownerKey.PublicKey);

        issuedCert.Should().NotBeNull();
    }
}

public class WalletContainer : IAsyncLifetime
{
    private readonly IContainer walletContainer;
    private readonly PostgreSqlContainer postgresContainer;
    private readonly INetwork network;

    public WalletContainer()
    {
        network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.2")
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .WithNetwork(network)
            .WithNetworkAliases("pg-service")
            .Build();

        const string connectionString = "Host=pg-service;Port=5432;Database=postgres;Username=postgres;Password=postgres";
        
        walletContainer = new ContainerBuilder()
            .WithImage("ghcr.io/project-origin/wallet-server:0.1.0-rc.4")
            .WithPortBinding(7890, 80)
            .WithCommand("--serve", "--migrate")
            .WithEnvironment("ConnectionStrings__Database", connectionString)
            .WithEnvironment("ServiceOptions__EndpointAddress", "http://localhost:7890/")
            .WithNetwork(network)
            .Build();
    }

    public string WalletUrl => new UriBuilder("http", walletContainer.Hostname, walletContainer.GetMappedPublicPort(80)).Uri.ToString();

    public async Task InitializeAsync()
    {
        await network.CreateAsync();

        await postgresContainer.StartAsync();

        await walletContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            postgresContainer.DisposeAsync().AsTask(),
            walletContainer.DisposeAsync().AsTask());
        await network.DeleteAsync();
    }
}
