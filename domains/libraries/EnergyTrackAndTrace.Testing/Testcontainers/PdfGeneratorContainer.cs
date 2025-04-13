using System.Net;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace EnergyTrackAndTrace.Testing.Testcontainers;

public class PdfGeneratorContainer : IAsyncLifetime
{
    private const int PdfServicePort = 8080;
    private IContainer _container = null!;

    public string Url { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        // Build from Dockerfile
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "html-pdf-generator")
            .WithDockerfile("Dockerfile")
            .Build();

        await image.CreateAsync();

        // Configure container
        _container = new ContainerBuilder()
            .WithImage(image)
            .WithPortBinding(PdfServicePort, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Server started"))
            .Build();

        await _container.StartAsync();

        // Get dynamic port
        Url = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(PdfServicePort)}";
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
