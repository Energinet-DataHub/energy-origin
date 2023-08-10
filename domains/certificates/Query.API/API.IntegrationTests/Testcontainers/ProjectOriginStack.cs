extern alias registryConnector;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ProjectOriginClients;
using registryConnector::RegistryConnector.Worker;
using Testcontainers.PostgreSql;

namespace API.IntegrationTests.Testcontainers;

public class ProjectOriginStack : RegistryFixture
{
    private readonly Lazy<IContainer> walletContainer;
    private readonly PostgreSqlContainer postgresContainer;

    public ProjectOriginStack()
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
            var connectionString = $"Host={postgresContainer.IpAddress};Port=5432;Database=postgres;Username=postgres;Password=postgres";

            // Get an available port from system and use that as the host port
            var udp = new UdpClient(0, AddressFamily.InterNetwork);
            var hostPort = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;

            return new ContainerBuilder()
                .WithImage($"ghcr.io/project-origin/wallet-server:{WalletVersion.Get()}")
                .WithPortBinding(hostPort, GrpcPort)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("ConnectionStrings__Database", connectionString)
                .WithEnvironment("ServiceOptions__EndpointAddress", $"http://localhost:{hostPort}/")
                .WithEnvironment($"RegistryUrls__{RegistryName}", RegistryContainerUrl)
                .WithEnvironment("VerifySlicesWorkerOptions__SleepTime", "00:00:01")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(GrpcPort))
                //.WithEnvironment("Logging__LogLevel__Default", "Trace")
                .Build();
        });
    }

    public ProjectOriginOptions Options => new()
    {
        RegistryName = RegistryName,
        Dk1IssuerPrivateKeyPem = Encoding.UTF8.GetBytes(Dk1IssuerKey.ExportPkixText()),
        Dk2IssuerPrivateKeyPem = Encoding.UTF8.GetBytes(Dk2IssuerKey.ExportPkixText()),
        RegistryUrl = RegistryUrl,
        WalletUrl = WalletUrl
    };

    public string WalletUrl => new UriBuilder("http", walletContainer.Value.Hostname, walletContainer.Value.GetMappedPublicPort(GrpcPort)).Uri.ToString();

    public override async Task InitializeAsync()
    {
        await Task.WhenAll(base.InitializeAsync(), postgresContainer.StartAsync());
        await walletContainer.Value.StartAsync();
    }

    public override async Task DisposeAsync() =>
        await Task.WhenAll(
            base.DisposeAsync(),
            postgresContainer.DisposeAsync().AsTask(),
            walletContainer.Value.DisposeAsync().AsTask());
}
