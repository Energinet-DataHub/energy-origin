extern alias registryConnector;
using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NSec.Cryptography;
using registryConnector::RegistryConnector.Worker;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

public class ProjectOriginRegistryContainer : IAsyncLifetime
{
    private readonly IContainer container;
    private readonly Key privateKey;

    public ProjectOriginRegistryContainer()
    {
        privateKey = Key.Create(SignatureAlgorithm.Ed25519, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

        container = new ContainerBuilder()
            .WithImage("ghcr.io/project-origin/electricity-server:0.1.0-alpha.19")
            .WithPortBinding(80, true)
            .WithEnvironment("Issuers__DK1", Convert.ToBase64String(privateKey.PublicKey.Export(KeyBlobFormat.RawPublicKey)))
            .WithEnvironment("REGISTRIES__RegistryA__VERIFIABLEEVENTSTORE__BATCHSIZEEXPONENT", "0")
            .WithEnvironment("REGISTRIES__RegistryA__VERIFIABLEEVENTSTORE__EVENTSTORE__TYPE", "inMemory")
            .WithEnvironment("IMMUTABLELOG__TYPE", "log")
            .Build();
    }

    public RegistryOptions Options =>
        new()
        {
            Url = new UriBuilder("http", container.Hostname, container.GetMappedPublicPort(80)).Uri.ToString(),
            IssuerPrivateKeyPem = privateKey.Export(KeyBlobFormat.PkixPrivateKeyText)
        };

    public async Task InitializeAsync() => await container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}
