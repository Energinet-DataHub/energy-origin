extern alias registryConnector;
using System;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

public class RegistryFixture : IAsyncLifetime
{
    private const string registryImage = "ghcr.io/project-origin/registry-server:0.2.0-rc.17";
    private const string electricityVerifierImage = "ghcr.io/project-origin/electricity-server:0.2.0-rc.17";
    protected const int GrpcPort = 80;
    private const string registryName = "TestRegistry";

    private readonly Lazy<IContainer> registryContainer;
    private readonly IContainer verifierContainer;

    public const string RegistryName = registryName;
    public IPrivateKey Dk1IssuerKey { get; init; }
    public IPrivateKey Dk2IssuerKey { get; init; }
    public string RegistryUrl => $"http://{registryContainer.Value.Hostname}:{registryContainer.Value.GetMappedPublicPort(GrpcPort)}";
    protected string RegistryContainerUrl => $"http://{registryContainer.Value.IpAddress}:{GrpcPort}";

    public RegistryFixture()
    {
        Dk1IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();
        Dk2IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        verifierContainer = new ContainerBuilder()
                .WithImage(electricityVerifierImage)
                .WithPortBinding(GrpcPort, true)
                .WithEnvironment("Issuers__DK1", Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk1IssuerKey.PublicKey.ExportPkixText())))
                .WithEnvironment("Issuers__DK2", Convert.ToBase64String(Encoding.UTF8.GetBytes(Dk2IssuerKey.PublicKey.ExportPkixText())))
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(GrpcPort)
                    )
                .Build();

        registryContainer = new Lazy<IContainer>(() =>
        {
            var verifierUrl = $"http://{verifierContainer.IpAddress}:{GrpcPort}";
            return new ContainerBuilder()
                .WithImage(registryImage)
                .WithPortBinding(GrpcPort, true)
                .WithEnvironment("Verifiers__project_origin.electricity.v1", verifierUrl)
                .WithEnvironment("RegistryName", registryName)
                .WithEnvironment("IMMUTABLELOG__TYPE", "log")
                .WithEnvironment("VERIFIABLEEVENTSTORE__BATCHSIZEEXPONENT", "0")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(GrpcPort)
                    )
                .Build();
        });
    }

    public virtual async Task InitializeAsync()
    {
        await verifierContainer.StartAsync()
            .ConfigureAwait(false);

        await registryContainer.Value.StartAsync()
            .ConfigureAwait(false);
    }

    public virtual async Task DisposeAsync()
    {
        if (registryContainer.IsValueCreated)
            await registryContainer.Value.StopAsync();
        await verifierContainer.StopAsync();
    }
}
