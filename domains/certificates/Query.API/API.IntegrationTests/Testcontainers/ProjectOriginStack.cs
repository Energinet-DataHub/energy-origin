using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace API.IntegrationTests.Testcontainers;

public class ProjectOriginStack : RegistryFixture
{
    private readonly Lazy<IContainer> walletContainer;
    private readonly Lazy<IContainer> stampContainer;
    private readonly PostgreSqlContainer walletPostgresContainer;
    private readonly PostgreSqlContainer stampPostgresContainer;

    private const int WalletHttpPort = 5000;
    private const int StampHttpPort = 5000;

    private const string PathBase = "/wallet-api";
    private const string StampPathBase = "/stamp-api";

    private const string WalletAlias = "wallet-container";

    public ProjectOriginStack()
    {
        walletPostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.2")
            .WithNetwork(Network)
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .Build();

        stampPostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.2")
            .WithNetwork(Network)
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .Build();

        walletContainer = new Lazy<IContainer>(() =>
        {
            var connectionString = $"Host={walletPostgresContainer.IpAddress};Port=5432;Database=postgres;Username=postgres;Password=postgres";

            // Get an available port from system and use that as the host port
            var udp = new UdpClient(0, AddressFamily.InterNetwork);
            var hostPort = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;

            return new ContainerBuilder()
                .WithImage("ghcr.io/project-origin/wallet-server:1.5.0")
                .WithNetwork(Network)
                .WithNetworkAliases(WalletAlias)
                .WithPortBinding(WalletHttpPort, true)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("ServiceOptions__EndpointAddress", $"http://{WalletAlias}:{WalletHttpPort}/")
                .WithEnvironment("ServiceOptions__PathBase", PathBase)
                .WithEnvironment($"RegistryUrls__{RegistryName}", RegistryContainerUrl)
                .WithEnvironment("Otlp__Endpoint", "http://foo")
                .WithEnvironment("Otlp__Enabled", "false")
                .WithEnvironment("ConnectionStrings__Database", connectionString)
                .WithEnvironment("MessageBroker__Type", "InMemory")
                .WithEnvironment("auth__type", "header")
                .WithEnvironment("auth__header__headerName", "wallet-owner")
                .WithEnvironment("auth__jwt__AllowAnyJwtToken", "true")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(WalletHttpPort))
                //.WithEnvironment("Logging__LogLevel__Default", "Trace")
                .Build();
        });

        stampContainer = new Lazy<IContainer>(() =>
        {
            var connectionString = $"Host={stampPostgresContainer.IpAddress};Port=5432;Database=postgres;Username=postgres;Password=postgres";

            // Get an available port from system and use that as the host port
            var udp = new UdpClient(0, AddressFamily.InterNetwork);
            var hostPort = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;

            return new ContainerBuilder()
                .WithImage("ghcr.io/project-origin/stamp:0.1.0")
                .WithNetwork(Network)
                .WithPortBinding(hostPort, StampHttpPort)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("RestApiOptions__PathBase", StampPathBase)
                .WithEnvironment("Otlp__Enabled", "false")
                .WithEnvironment("Retry__DefaultFirstLevelRetryCount", "5")
                .WithEnvironment("Retry__RegistryTransactionStillProcessingRetryCount", "100")
                .WithEnvironment($"Registry__RegistryUrls__{RegistryName}", RegistryContainerUrl)
                .WithEnvironment($"Registry__IssuerPrivateKeyPems__DK1", Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk1IssuerKey.ExportPkixText())))
                .WithEnvironment($"Registry__IssuerPrivateKeyPems__DK2", Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk1IssuerKey.ExportPkixText())))
                .WithEnvironment("ConnectionStrings__Database", connectionString)
                .WithEnvironment("MessageBroker__Type", "InMemory")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(StampHttpPort))
                //.WithEnvironment("Logging__LogLevel__Default", "Trace")
                .Build();
        });
    }

    public string WalletUrl => new UriBuilder("http", walletContainer.Value.Hostname, walletContainer.Value.GetMappedPublicPort(WalletHttpPort), PathBase).Uri.ToString();
    public string StampUrl => new UriBuilder("http", stampContainer.Value.Hostname, stampContainer.Value.GetMappedPublicPort(StampHttpPort), StampPathBase).Uri.ToString();

    public override async Task InitializeAsync()
    {
        await Task.WhenAll(base.InitializeAsync(), walletPostgresContainer.StartAsync(), stampPostgresContainer.StartAsync());
        await Task.WhenAll(walletContainer.Value.StartAsync(), stampContainer.Value.StartAsync());
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();

        await Task.WhenAll(
            walletPostgresContainer.DisposeAsync().AsTask(),
            walletContainer.Value.DisposeAsync().AsTask(),
            stampPostgresContainer.DisposeAsync().AsTask(),
            stampContainer.Value.DisposeAsync().AsTask());
    }
}
