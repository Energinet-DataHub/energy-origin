using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using EnergyTrackAndTrace.Testing.Extensions;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace EnergyTrackAndTrace.Testing.Testcontainers;

public class RegistryFixture : IAsyncLifetime
{
    private const string registryImage = "ghcr.io/project-origin/registry-server:2.0.3";
    private const string electricityVerifierImage = "ghcr.io/project-origin/electricity-server:1.3.2";
    protected const int GrpcPort = 5000;
    private const int RabbitMqHttpPort = 15672;
    private const string registryName = "TestRegistry";
    private const string RegistryAlias = "registry-container";
    private const string VerifierAlias = "verifier-container";
    private const string VerifierPostgresAlias = "verifier-postgres-container";
    private const string RabbitMqAlias = "rabbitmq-container";

    private readonly IContainer registryContainer;
    private readonly IContainer verifierContainer;
    private readonly global::Testcontainers.RabbitMq.RabbitMqContainer rabbitMqContainer;
    private readonly PostgreSqlContainer registryPostgresContainer;
    protected readonly INetwork Network;
    private readonly string rabbitMqImage = "rabbitmq:3.13-management";
    public const string RegistryName = registryName;
    public IPrivateKey Dk1IssuerKey { get; init; }
    public IPrivateKey Dk2IssuerKey { get; init; }
    public string RegistryUrl => $"http://{registryContainer.Hostname}:{registryContainer.GetMappedPublicPort(GrpcPort)}";
    protected string RegistryContainerUrl => $"http://{registryContainer.IpAddress}:{GrpcPort}";
    public RegistryFixture()
    {
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString())
            .Build();
        rabbitMqContainer = new RabbitMqBuilder()
            .WithImage(rabbitMqImage)
            .WithNetwork(Network)
            .WithNetworkAliases(RabbitMqAlias)
            .WithPortBinding(RabbitMqHttpPort, true)
            .Build();
        Dk1IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();
        Dk2IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        var configFile = Path.GetTempFileName() + ".yaml";
        File.WriteAllText(configFile, $"""
        registries:
          {registryName}:
            url: http://{RegistryAlias}:{GrpcPort}
        areas:
          DK1:
            issuerKeys:
              - publicKey: "{Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk1IssuerKey.PublicKey.ExportPkixText()))}"
          DK2:
            issuerKeys:
              - publicKey: "{Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk2IssuerKey.PublicKey.ExportPkixText()))}"
        """);

        verifierContainer = new ContainerBuilder()
                .WithImage(electricityVerifierImage)
                .WithNetwork(Network)
                .WithNetworkAliases(VerifierAlias)
                .WithResourceMapping(configFile, "/app/tmp/")
                .WithPortBinding(GrpcPort, true)
                .WithCommand("--serve")
                .WithEnvironment("Network__ConfigurationUri", "file:///app/tmp/" + Path.GetFileName(configFile))
                .WithWaitStrategy(Wait.ForUnixContainer().UntilGrpcEndpointIsReady(GrpcPort, "/"))
                // .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(GrpcPort, o => o.WithTimeout(TimeSpan.FromSeconds(10))))
                .Build();

        registryPostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithNetwork(Network)
            .WithNetworkAliases(VerifierPostgresAlias)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        registryContainer = new ContainerBuilder()
            .WithImage(registryImage)
            .WithNetwork(Network)
            .WithNetworkAliases(RegistryAlias)
            .WithPortBinding(GrpcPort, true)
            .WithCommand("--migrate", "--serve")
            .WithEnvironment("RegistryName", registryName)
            .WithEnvironment("Otlp__Enabled", "false")
            .WithEnvironment("Verifiers__project_origin.electricity.v1", $"http://{VerifierAlias}:{GrpcPort}")
            .WithEnvironment("IMMUTABLELOG__TYPE", "log")
            .WithEnvironment("BlockFinalizer__Interval", "00:00:05")
            .WithEnvironment("cache__TYPE", "InMemory")
            .WithEnvironment("RabbitMq__Hostname", RabbitMqAlias)
            .WithEnvironment("RabbitMq__AmqpPort", RabbitMqBuilder.RabbitMqPort.ToString())
            .WithEnvironment("RabbitMq__HttpApiPort", RabbitMqHttpPort.ToString())
            .WithEnvironment("RabbitMq__Username", RabbitMqBuilder.DefaultUsername)
            .WithEnvironment("RabbitMq__Password", RabbitMqBuilder.DefaultPassword)
            .WithEnvironment("TransactionProcessor__ServerNumber", "0")
            .WithEnvironment("TransactionProcessor__Servers", "1")
            .WithEnvironment("TransactionProcessor__Threads", "5")
            .WithEnvironment("TransactionProcessor__Weight", "10")
            .WithEnvironment("ConnectionStrings__Database", registryPostgresContainer.GetLocalConnectionString(VerifierPostgresAlias))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilGrpcEndpointIsReady(GrpcPort, "/"))
            .Build();
    }

    public virtual async ValueTask InitializeAsync()
    {
        await Network.CreateAsync();
        await rabbitMqContainer.StartWithLoggingAsync();
        await verifierContainer.StartWithLoggingAsync();
        await registryPostgresContainer.StartWithLoggingAsync();
        await registryContainer.StartWithLoggingAsync();
    }

    public virtual async ValueTask DisposeAsync()
    {
        await registryContainer.StopAsync();
        await registryPostgresContainer.StopAsync();
        await rabbitMqContainer.StopAsync();
        await verifierContainer.StopAsync();
        await Network.DisposeAsync();
    }
}
