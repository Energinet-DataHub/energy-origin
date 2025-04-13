using System.IO;
using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using EnergyOrigin.Setup.RabbitMq;
using Xunit;

namespace API.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class PdfIntegrationTestCollection : ICollectionFixture<PdfIntegrationFixture>
{
    public const string CollectionName = "PdfIntegrationTestCollection";
}

public class PdfIntegrationFixture : IAsyncLifetime
{
    private const int Port = 8080;
    private IContainer _pdfContainer = null!;
    private IFutureDockerImage _image = null!;

    public string PdfUrl { get; private set; } = null!;
    public TransferAgreementsApiWebApplicationFactory Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        // 1. Build the Docker image from the Dockerfile
        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "html-pdf-generator")
            .WithDockerfile("Dockerfile")
            .WithName("html-pdf-generator:test")
            .WithBuildArgument("RESOURCE_REAPER_SESSION_ID", ResourceReaper.DefaultSessionId.ToString("D"))
            .WithDeleteIfExists(true)
            .Build();

        await _image.CreateAsync();

        // 2. Create and start the container
        _pdfContainer = new ContainerBuilder()
            .WithImage(_image)
            .WithPortBinding(Port, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPath("/health")
                    .ForStatusCode(HttpStatusCode.OK)))
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await _pdfContainer.StartAsync();

        // 3. Get the mapped port and create the PDF URL
        var mappedPort = _pdfContainer.GetMappedPublicPort(Port);
        PdfUrl = $"http://localhost:{mappedPort}/generate-pdf";

        // 4. Create and configure the factory with the PDF URL
        Factory = new TransferAgreementsApiWebApplicationFactory
        {
            PdfUrl = PdfUrl,
            ConnectionString = "Host=fake;Port=5432;Database=test;Username=postgres;Password=postgres",
            CvrBaseUrl = "http://fake-cvr",
            RabbitMqOptions = new RabbitMqOptions
            {
                Host = "localhost",
                Port = 5672,
                Username = "guest",
                Password = "guest"
            }
        };

        // 5. Start the factory to apply the configuration
        Factory.Start();
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _pdfContainer.DisposeAsync();
    }
}
