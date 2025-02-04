using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using Testcontainers.RabbitMq;
using Xunit;

namespace EnergyOrigin.WalletClient.Tests.Testcontainers;

public class RegistryFixture : IAsyncLifetime
{
    private const string registryImage = "ghcr.io/project-origin/registry-server:1.3.0";
    private const string electricityVerifierImage = "ghcr.io/project-origin/electricity-server:1.1.0";
    protected const int GrpcPort = 5000;
    private const int RabbitMqHttpPort = 15672;
    private const string registryName = "TestRegistry";
    private const string RegistryAlias = "registry-container";
    private const string VerifierAlias = "verifier-container";
    private const string RabbitMqAlias = "rabbitmq-container";

    private readonly IContainer registryContainer;
    private readonly IContainer verifierContainer;
    private readonly RabbitMqContainer rabbitMqContainer;
    protected readonly INetwork Network;
    private readonly IFutureDockerImage rabbitMqImage;

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

        rabbitMqImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetProjectDirectory(), string.Empty)
            .WithDockerfile("rabbitmq.dockerfile")
            .Build();

        rabbitMqContainer = new RabbitMqBuilder()
            .WithImage(rabbitMqImage)
            .WithNetwork(Network)
            .WithNetworkAliases(RabbitMqAlias)
            .WithPortBinding(RabbitMqHttpPort, true)
            .Build();

        Dk1IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();
        Dk2IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        verifierContainer = new ContainerBuilder()
                .WithImage(electricityVerifierImage)
                .WithNetwork(Network)
                .WithNetworkAliases(VerifierAlias)
                .WithPortBinding(GrpcPort, true)
                .WithCommand("--serve")
                .WithEnvironment("Issuers__DK1", Convert.ToBase64String(Encoding.UTF8.GetBytes((string)Dk1IssuerKey.PublicKey.ExportPkixText())))
                .WithEnvironment("Issuers__DK2", Convert.ToBase64String(Encoding.UTF8.GetBytes((string)Dk2IssuerKey.PublicKey.ExportPkixText())))
                .WithEnvironment($"Registries__{RegistryName}__Address", $"http://{RegistryAlias}:{GrpcPort}")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(GrpcPort)
                    )
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
            .WithEnvironment("PERSISTANCE__TYPE", "in_memory")
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
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(GrpcPort)
            )
            .Build();
    }

    public virtual async Task InitializeAsync()
    {
        await rabbitMqImage.CreateAsync();
        await Network.CreateAsync();
        await rabbitMqContainer.StartAsync();
        await verifierContainer.StartAsync();
        await registryContainer.StartAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await registryContainer.StopAsync();
        await rabbitMqContainer.StopAsync();
        await verifierContainer.StopAsync();
        await Network.DisposeAsync();
    }
}
