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
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithExposedPort(5432)
            .WithPortBinding(5432, true)
            .Build();

        walletContainer = new Lazy<IContainer>(() =>
        {
            var postgresConnectionString = $"Host={postgresContainer.IpAddress};Port=5432;Database=postgres;Username=postgres;Password=postgres";

            return new ContainerBuilder()
                .WithImage("ghcr.io/project-origin/wallet-server:0.1.3")
                .WithPortBinding(80, true)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("ConnectionStrings__Database", postgresConnectionString)
                .WithEnvironment("ServiceOptions__EndpointAddress", $"http://postgres:5432/")
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

    public async Task DisposeAsync()
    {
        await Task.WhenAll(walletContainer.Value.DisposeAsync().AsTask(),
            postgresContainer.DisposeAsync().AsTask());
    }
}
