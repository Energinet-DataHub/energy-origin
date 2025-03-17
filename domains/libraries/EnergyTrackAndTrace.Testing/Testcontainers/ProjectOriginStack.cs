using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EnergyTrackAndTrace.Testing.Extensions;
using Testcontainers.PostgreSql;

namespace EnergyTrackAndTrace.Testing.Testcontainers;

public class ProjectOriginStack : RegistryFixture
{
    private readonly Lazy<IContainer> _walletContainer;
    private readonly Lazy<IContainer> _stampContainer;
    private readonly PostgreSqlContainer _walletPostgresContainer;
    private readonly PostgreSqlContainer _stampPostgresContainer;

    private const int WalletHttpPort = 5000;
    private const int StampHttpPort = 5000;

    private const string PathBase = "/wallet-api";
    private const string StampPathBase = "/stamp-api";

    private const string WalletAlias = "wallet-container";
    private const string StampAlias = "stamp-container";

    public ProjectOriginStack()
    {
        _walletPostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.2")
            .WithNetwork(Network)
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .Build();

        _stampPostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.2")
            .WithNetwork(Network)
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .Build();


        _walletContainer = new Lazy<IContainer>(() =>
        {
            var connectionString = $"Host={_walletPostgresContainer.IpAddress};Port=5432;Database=postgres;Username=postgres;Password=postgres";

            var networkOptions = new NetworkOptions
            {
                DaysBeforeCertificatesExpire = 60
            };
            networkOptions.Registries.Add(RegistryName, new RegistryInfo
            {
                Url = RegistryContainerUrl,
            });
            networkOptions.Areas.Add("DK1", new AreaInfo
            {
                IssuerKeys = new List<KeyInfo>{
                    new (){
                        PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk1IssuerKey.ExportPkixText()))
                    }
                }
            });
            networkOptions.Areas.Add("DK2", new AreaInfo
            {
                IssuerKeys = new List<KeyInfo>{
                    new (){
                        PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk2IssuerKey.ExportPkixText()))
                    }
                }
            });
            networkOptions.Issuers.Add(StampAlias, new IssuerInfo
            {
                StampUrl = new UriBuilder("http", StampAlias, StampHttpPort, StampPathBase).Uri.ToString()
            });

            var configFile = networkOptions.ToTempYamlFile();

            return new ContainerBuilder()
                .WithImage("ghcr.io/project-origin/vault:2.0.1")
                .WithNetwork(Network)
                .WithNetworkAliases(WalletAlias)
                .WithResourceMapping(configFile, "/app/tmp/")
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
                .WithEnvironment("Retry__RegistryTransactionStillProcessingRetryCount", "5")
                .WithEnvironment("Retry__RegistryTransactionStillProcessingInitialIntervalSeconds", "1")
                .WithEnvironment("Retry__RegistryTransactionStillProcessingIntervalIncrementSeconds", "5")
                .WithEnvironment("Job__CheckForWithdrawnCertificatesIntervalInSeconds", "5")
                .WithEnvironment("Job__ExpireCertificatesIntervalInSeconds", "5")
                .WithEnvironment("network__ConfigurationUri", "file:///app/tmp/" + Path.GetFileName(configFile))
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(WalletHttpPort))
                //.WithEnvironment("Logging__LogLevel__Default", "Trace")
                .Build();
        });

        _stampContainer = new Lazy<IContainer>(() =>
        {
            var connectionString = $"Host={_stampPostgresContainer.IpAddress};Port=5432;Database=postgres;Username=postgres;Password=postgres";

            // Get an available port from system and use that as the host port
            var udp = new UdpClient(0, AddressFamily.InterNetwork);
            var hostPort = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;

            return new ContainerBuilder()
                .WithImage("ghcr.io/project-origin/stamp:5.0.1")
                .WithNetwork(Network)
                .WithNetworkAliases(StampAlias)
                .WithPortBinding(hostPort, StampHttpPort)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("RestApiOptions__PathBase", StampPathBase)
                .WithEnvironment("Otlp__Enabled", "false")
                .WithEnvironment("Retry__DefaultFirstLevelRetryCount", "5")
                .WithEnvironment("Retry__RegistryTransactionStillProcessingRetryCount", "10")
                .WithEnvironment("Retry__RegistryTransactionStillProcessingInitialIntervalSeconds", "1")
                .WithEnvironment("Retry__RegistryTransactionStillProcessingIntervalIncrementSeconds", "5")
                .WithEnvironment($"Registries__0__name", RegistryName)
                .WithEnvironment($"Registries__0__address", RegistryContainerUrl)
                .WithEnvironment($"IssuerPrivateKeyPems__DK1", Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk1IssuerKey.ExportPkixText())))
                .WithEnvironment($"IssuerPrivateKeyPems__DK2", Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk1IssuerKey.ExportPkixText())))
                .WithEnvironment("ConnectionStrings__Database", connectionString)
                .WithEnvironment("MessageBroker__Type", "InMemory")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(StampHttpPort))
                //.WithEnvironment("Logging__LogLevel__Default", "Trace")
                .Build();
        });
    }

    public string WalletUrl =>
        new UriBuilder("http", _walletContainer.Value.Hostname, _walletContainer.Value.GetMappedPublicPort(WalletHttpPort), PathBase).Uri.ToString();

    public string StampUrl =>
        new UriBuilder("http", _stampContainer.Value.Hostname, _stampContainer.Value.GetMappedPublicPort(StampHttpPort), StampPathBase).Uri.ToString();

    public override async Task InitializeAsync()
    {
        await Task.WhenAll(base.InitializeAsync(), _walletPostgresContainer.StartAsync(), _stampPostgresContainer.StartAsync());
        await Task.WhenAll(_walletContainer.Value.StartAsync(), _stampContainer.Value.StartAsync());
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();

        await Task.WhenAll(
            _walletPostgresContainer.DisposeAsync().AsTask(),
            _walletContainer.Value.DisposeAsync().AsTask(),
            _stampPostgresContainer.DisposeAsync().AsTask(),
            _stampContainer.Value.DisposeAsync().AsTask());
    }
}
