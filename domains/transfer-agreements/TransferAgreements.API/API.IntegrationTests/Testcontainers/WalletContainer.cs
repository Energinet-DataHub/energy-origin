using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

//TNE 20/07/2023: Not configured with project origin registry, since it's not needed yet
public class WalletContainer : IAsyncLifetime
{
    private readonly Lazy<IContainer> walletContainer;
    private readonly PostgreSqlContainer postgresContainer;

    public WalletContainer()
    {
        postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.2")
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .Build();

        walletContainer = new Lazy<IContainer>(() =>
        {
            var postgresConnectionString = $"Host={postgresContainer.IpAddress};Port=5432;Database=postgres;Username=postgres;Password=postgres";

            return new ContainerBuilder()
            .WithImage("ghcr.io/project-origin/wallet-server:0.1.1")
            .WithPortBinding(7890, 80)
            .WithCommand("--serve", "--migrate")
            .WithEnvironment("ConnectionStrings__Database", postgresConnectionString)
            .WithEnvironment("ServiceOptions__EndpointAddress", "http://localhost:7890/")
            .WithEnvironment("VerifySlicesWorkerOptions__SleepTime", "00:00:01")
            .Build();
        });
    }
    public string WalletUrl => new UriBuilder("http", walletContainer.Value.Hostname, walletContainer.Value.GetMappedPublicPort(80)).Uri.ToString();

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();
        await walletContainer.Value.StartAsync();
    }

    public Task DisposeAsync() =>
        Task.WhenAll(walletContainer.Value.DisposeAsync().AsTask(),
            postgresContainer.DisposeAsync().AsTask());
}
