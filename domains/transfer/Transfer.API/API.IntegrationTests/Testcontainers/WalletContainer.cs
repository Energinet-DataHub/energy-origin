using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

public class WalletContainer : IAsyncLifetime
{
    private readonly Lazy<IContainer> walletContainer;
    private readonly PostgreSqlContainer postgresContainer;

    public WalletContainer()
    {
        postgresContainer = new PostgreSqlBuilder()
            .WithPortBinding(5432, true)
            .Build();

        walletContainer = new Lazy<IContainer>(() =>
        {
            var postgresConnectionString = postgresContainer.GetConnectionString()
                .Replace($"Host={postgresContainer.Hostname}", $"Host={postgresContainer.IpAddress}")
                .Replace($"Port={postgresContainer.GetMappedPublicPort(PostgreSqlBuilder.PostgreSqlPort)}", $"Port={PostgreSqlBuilder.PostgreSqlPort}");

            return new ContainerBuilder()
                .WithImage("ghcr.io/project-origin/wallet-server:0.2.1")
                .WithPortBinding(80, true)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("ConnectionStrings__Database", postgresConnectionString)
                //TODO - This should be implemented correctly, right now it apparently doesn't use it https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1688
                .WithEnvironment("ServiceOptions__EndpointAddress", $"http://whatever.com/")
                .WithEnvironment("MessageBroker__Type", "InMemory")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(80)
                )
                .Build();
        });
    }

    public string WalletUrl => new UriBuilder("http", walletContainer.Value.Hostname, walletContainer.Value.GetMappedPublicPort(80)).Uri.ToString();

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();
        await walletContainer.Value.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(walletContainer.Value.DisposeAsync().AsTask(),
            postgresContainer.DisposeAsync().AsTask());
    }
}
