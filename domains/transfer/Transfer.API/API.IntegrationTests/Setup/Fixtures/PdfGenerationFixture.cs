using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.UnitTests.ReportGenerator.Utilities;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Grpc.Core;
using Meteringpoint.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace API.IntegrationTests.Setup.Fixtures;

[CollectionDefinition(CollectionName)]
public class PdfTestCollection : ICollectionFixture<PdfGenerationFixture>
{
    public const string CollectionName = "PdfTestCollection";
}

public sealed class PdfGenerationFixture : IAsyncLifetime
{
    public PdfGenerationFixture()
    {
        _to = _from.AddYears(1);
        Factory = new TransferAgreementsApiWebApplicationFactory();
        RabbitMqContainer = new RabbitMqContainer();
    }

    public TransferAgreementsApiWebApplicationFactory Factory { get; private set; } = default!;
    public PostgresContainer                        Pg      { get; private set; } = default!;
    public RabbitMqContainer RabbitMqContainer { get; private set; }

    public IContainer                               Pdf     { get; private set; } = default!;
    private IWalletClient Wallet => Factory.WalletClientMock;
    private readonly API.Transfer.Api.Services.IConsumptionService _consumption =
        Substitute.For<API.Transfer.Api.Services.IConsumptionService>();
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _meteringPointClient =
        Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
    private readonly IDataHubFacadeClient _dhFacadeClient = Substitute.For<IDataHubFacadeClient>();
    private readonly IDataHub3Client _dh3Client = Substitute.For<IDataHub3Client>();


    public OrganizationId _orgId { get; } = OrganizationId.Create(Guid.NewGuid());

    private const int  Seed = 11;
    private readonly DateTimeOffset _from = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _to;

    public async ValueTask InitializeAsync()
    {
        Pg = new PostgresContainer();
        await Pg.InitializeAsync();
        await RabbitMqContainer.InitializeAsync();
        ConfigureMeteringPointClientMock();

        Pdf = await StartPdfContainerAsync();

        var pdfEndpoint = $"http://{Pdf.Hostname}:{Pdf.GetMappedPublicPort(8080)}";

        ConfigureConsumptionMock();
        ConfigureWalletMock();
        Factory.RabbitMqOptions = RabbitMqContainer.Options;
        Factory = new TransferAgreementsApiWebApplicationFactory
        {
            DataHub3Url = "http://mock-dh3", // non-existent but valid format
            DataHubFacadeUrl = "http://mock-facade",
            DataHubFacadeGrpcUrl = "http://mock-facade-grpc",
            ConnectionString = Pg.ConnectionString,
            PdfUrl           = pdfEndpoint
        };

        Factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(s =>
            {
                s.RemoveAll<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
                s.AddSingleton(_meteringPointClient);
                s.RemoveAll<IDataHub3Client>();
                s.AddSingleton(_dh3Client);

                s.RemoveAll<IDataHubFacadeClient>();
                s.AddSingleton(_dhFacadeClient);
                s.RemoveAll<API.Transfer.Api.Services.IConsumptionService>();
                s.AddSingleton(_consumption);
            });
        });

        Factory.Start();

        using var scope = Factory.Services.CreateScope();
    }

    private static async Task<IContainer> StartPdfContainerAsync()
    {
        var baseDir = AppContext.BaseDirectory;
        var pdfSrc = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "html-pdf-generator"));

        var pdfImage = new ImageFromDockerfileBuilder()
            .WithName($"pdf-gen:{Guid.NewGuid():N}")
            .WithDockerfileDirectory(new CommonDirectoryPath(pdfSrc), string.Empty)
            .WithDockerfile("Dockerfile")
            .WithCleanUp(false)
            .Build();

        await pdfImage.CreateAsync();

        var container = new ContainerBuilder()
            .WithImage(pdfImage)
            .WithPortBinding(8080, true)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole()) // live logs ☺
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(8080)
                    .UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("health")))
            .Build();

        await container.StartAsync();
        return container;
    }

    private void ConfigureConsumptionMock()
    {
        var hours = MockedDataGenerators.GenerateMockConsumption(Seed);

        _consumption.GetAverageHourlyConsumption(
                       _orgId,
                       Arg.Is<DateTimeOffset>(d => d == _from),
                       Arg.Is<DateTimeOffset>(d => d == _to),
                       Arg.Any<CancellationToken>())
                   .Returns(hours);
    }

    private void ConfigureMeteringPointClientMock()
    {
        _meteringPointClient.GetOwnedMeteringPointsAsync(
            Arg.Any<OwnedMeteringPointsRequest>(),
            Arg.Any<Metadata>(),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>()
        ).Returns(_ => new AsyncUnaryCall<MeteringPointsResponse>(
            Task.FromResult(new MeteringPointsResponse()),
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { }
        ));
    }

    private void ConfigureWalletMock()
    {
        var strict = MockedDataGenerators.GenerateMockClaims(Seed, _from, _to, strictHourlyOnly: true);
        var all    = MockedDataGenerators.GenerateMockClaims(Seed, _from, _to, strictHourlyOnly: false);

        Wallet.GetClaims(
                  _orgId.Value, _from, _to, TimeMatch.Hourly, Arg.Any<CancellationToken>())
              .Returns(new ResultList<Claim>
              {
                  Result   = strict,
                  Metadata = new PageInfo
                  {
                      Count  = strict.Count,
                      Offset = 0,
                      Limit  = strict.Count,
                      Total  = strict.Count
                  }
              });

        Wallet.GetClaims(
                  _orgId.Value, _from, _to, TimeMatch.All, Arg.Any<CancellationToken>())
              .Returns(new ResultList<Claim>
              {
                  Result   = all,
                  Metadata = new PageInfo
                  {
                      Count  = all.Count,
                      Offset = 0,
                      Limit  = all.Count,
                      Total  = all.Count
                  }
              });
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Pg.DisposeAsync();
        await Pdf.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
    }
}
