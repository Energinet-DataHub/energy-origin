using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
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
    private static ConcurrentBag<int> ports = new ConcurrentBag<int>(Enumerable.Range(7000, 7999));
    private int hostPort;


    public WalletContainer()
    {
        if (!ports.TryTake(out hostPort))
        {
            throw new InvalidOperationException("No available ports.");
        }

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
                .WithImage("ghcr.io/project-origin/wallet-server:0.1.3")
                .WithPortBinding(hostPort, 80)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("ConnectionStrings__Database", postgresConnectionString)
                .WithEnvironment("ServiceOptions__EndpointAddress", $"http://localhost:{hostPort}/")
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
        ports.Add(hostPort);

        await Task.WhenAll(walletContainer.Value.DisposeAsync().AsTask(),
            postgresContainer.DisposeAsync().AsTask());
    }
}
