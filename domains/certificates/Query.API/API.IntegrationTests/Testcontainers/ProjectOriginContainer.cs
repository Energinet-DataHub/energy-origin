extern alias registryConnector;
using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NSec.Cryptography;
using registryConnector::RegistryConnector.Worker;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

public class ProjectOriginContainer : IAsyncLifetime
{
    private readonly IContainer container;

    private string privateKeyStr ="LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1DNENBUUF3QlFZREsyVndCQ0lFSUJhb2FjVHVWL05ub3ROQTBlVzJxbFJZZ3Q2WTRsaWlXSzV5VDRFZ3JKR20KLS0tLS1FTkQgUFJJVkFURSBLRVktLS0tLQo=";

    public ProjectOriginContainer()
    {
        var privateKey = Key.Import(SignatureAlgorithm.Ed25519, Convert.FromBase64String(privateKeyStr),
            KeyBlobFormat.PkixPrivateKeyText);

        container = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("ghcr.io/project-origin/electricity-server:0.1.0-alpha.18")
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
            IssuerPrivateKeyPem = Convert.FromBase64String(privateKeyStr)
        };

    public async Task InitializeAsync() => await container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}
