using System;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests;

public class HoleTest : IClassFixture<RegistryConnectorApplicationFactory>, IClassFixture<WalletContainer>
{
    private readonly RegistryConnectorApplicationFactory factory;
    private readonly WalletContainer walletContainer;

    public HoleTest(RegistryConnectorApplicationFactory factory, WalletContainer walletContainer)
    {
        this.factory = factory;
        this.walletContainer = walletContainer;
    }

    [Fact]
    public async Task Test1()
    {
        factory.Start();

        await Task.Delay(TimeSpan.FromSeconds(10));

        walletContainer.Url.Should().Be("http://127.0.0.1:7890/");
    }
}

public class WalletContainer : IAsyncLifetime
{
    private readonly IContainer walletContainer;
    private readonly PostgreSqlContainer postgreSqlContainer;
    private readonly INetwork network;

    public WalletContainer()
    {
        network = new NetworkBuilder().WithName(Guid.NewGuid().ToString("D")).Build();

        postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.2")
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .WithNetwork(network)
            .WithNetworkAliases("postgres")
            .Build();

        const string connectionString = "Host=postgres;Port=5432;Database=postgres;Username=postgres;Password=postgres";

        walletContainer = new ContainerBuilder()
            .WithImage("ghcr.io/project-origin/wallet-server:0.1.0-rc.4")
            .WithPortBinding(7890, 80)
            .WithEnvironment("CONNECTIONSTRINGS__DATABASE", connectionString)
            .WithNetwork(network)
            .Build();
    }

    public string Url => new UriBuilder("http", walletContainer.Hostname, walletContainer.GetMappedPublicPort(80)).Uri.ToString();

    public async Task InitializeAsync()
    {
        await network.CreateAsync();

        await postgreSqlContainer.StartAsync();
        
        await walletContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await postgreSqlContainer.DisposeAsync();
        await walletContainer.DisposeAsync();
    }
}
