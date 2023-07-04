extern alias registryConnector;
using System;
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

            // The host port is fixed due to the fact that it used in the value for "ServiceOptions__EndpointAddress"
            // There is a chance for port collision with the host ports assigned by Testcontainers
            return new ContainerBuilder()
                .WithImage("ghcr.io/project-origin/wallet-server:0.1.0-rc.7")
                .WithPortBinding(7890, GrpcPort)
                .WithCommand("--serve", "--migrate")
                .WithEnvironment("ConnectionStrings__Database", connectionString)
                .WithEnvironment("ServiceOptions__EndpointAddress", "http://localhost:7890/")
                .WithEnvironment($"RegistryUrls__{RegistryName}", RegistryContainerUrl)
                .WithEnvironment("VerifySlicesWorkerOptions__SleepTime", "00:00:01")
                //.WithEnvironment("Logging__LogLevel__Default", "Trace")
                .Build();
        });
    }

    public ProjectOriginOptions Options => new()
    {
        RegistryName = RegistryName,
        Dk1IssuerPrivateKeyPem = IssuerKey.ExportPkixText(),
        Dk2IssuerPrivateKeyPem = IssuerKey.ExportPkixText(),
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
