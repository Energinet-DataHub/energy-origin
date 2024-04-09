extern alias registryConnector;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using registryConnector::RegistryConnector.Worker;
using Testcontainers.PostgreSql;

namespace API.IntegrationTests.Testcontainers;

public class ProjectOriginStack : RegistryFixture
{
    private readonly Lazy<IContainer> walletContainer;
    private readonly PostgreSqlContainer postgresContainer;

    private const int WalletGrpcPort = 5001;

    public ProjectOriginStack()
    {
        postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.2")
            .WithNetwork(Network)
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
                .WithImage("ghcr.io/project-origin/wallet-server:0.10.3")
                .WithNetwork(Network)
                .WithPortBinding(hostPort, WalletGrpcPort)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("ServiceOptions__EndpointAddress", $"http://localhost:{hostPort}/")
                .WithEnvironment($"RegistryUrls__{RegistryName}", RegistryContainerUrl)
                .WithEnvironment("RestApiOptions__PathBase", "/wallet-api")
                .WithEnvironment("Otlp__Endpoint", "http://foo")
                .WithEnvironment("Otlp__Enabled", "false")
                .WithEnvironment("ConnectionStrings__Database", connectionString)
                .WithEnvironment("MessageBroker__Type", "InMemory")
                .WithEnvironment("jwt__AllowAnyJwtToken", "true")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(WalletGrpcPort))
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

    public string WalletUrl => new UriBuilder("http", walletContainer.Value.Hostname, walletContainer.Value.GetMappedPublicPort(WalletGrpcPort)).Uri.ToString();

    public override async Task InitializeAsync()
    {
        await Task.WhenAll(base.InitializeAsync(), postgresContainer.StartAsync());
        await walletContainer.Value.StartAsync();
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();

        await Task.WhenAll(
            postgresContainer.DisposeAsync().AsTask(),
            walletContainer.Value.DisposeAsync().AsTask());
    }
}
