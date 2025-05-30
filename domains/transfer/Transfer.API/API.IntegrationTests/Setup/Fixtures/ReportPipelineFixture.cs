// File: API.IntegrationTests/Setup/Fixtures/ReportPipelineFixture.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.UnitTests.ReportGenerator.Utilities;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests.Setup.Fixtures;

public sealed class ReportPipelineFixture : IAsyncLifetime
{
    /* ---------------- external objects ------------------- */
    public TransferAgreementsApiWebApplicationFactory Factory { get; private set; } = default!;
    public PostgreSqlContainer                         Pg      { get; private set; } = default!;
    public IContainer                                  Pdf     { get; private set; } = default!;
    public IWalletClient                               Wallet  { get; } = Substitute.For<IWalletClient>();
    public API.Transfer.Api.Services.IConsumptionService Consumption { get; } =
        Substitute.For<API.Transfer.Api.Services.IConsumptionService>();

    /* ----------------------------------------------------- */
    public async ValueTask InitializeAsync()
    {
        /* 1️⃣  Postgres ------------------------------------------------ */
        Pg = new PostgreSqlBuilder()
                .WithUsername("test")
                .WithPassword("test")
                .WithDatabase("reportdb")
                .Build();

        await Pg.StartAsync();

        /* 2️⃣  PDF generator ----------------------------------------- */
        //
        // Build the Node / Playwright image from the Dockerfile that
        // lives in ‘PdfGenerator/’.
        //
        var pdfSrc = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "PdfGenerator"));

        var pdfImage = new ImageFromDockerfileBuilder()
            .WithName($"pdf-generator:{Guid.NewGuid():N}")
            .WithDockerfileDirectory(pdfSrc, string.Empty) // build-context root = PdfGenerator/
            .WithDockerfile("Dockerfile")
            .WithCleanUp(true)                              // auto-delete image after the test run
            .Build();

        await pdfImage.CreateAsync();

        Pdf = new ContainerBuilder()
            .WithImage(pdfImage)                            // use freshly-built image
            .WithPortBinding(8080, true)                    // random host-port
            .WithWaitStrategy(
                Wait.ForUnixContainer()                     // wait until HTTP GET /generate-pdf returns 200
                    .UntilHttpRequestIsSucceeded(r => r
                        .ForPort(8080)
                        .ForPath("generate-pdf")))
            .Build();

        await Pdf.StartAsync();

        var pdfEndpoint = $"http://{Pdf.Hostname}:{Pdf.GetMappedPublicPort(8080)}/generate-pdf";

        /* 3️⃣  Up-stream service mocks ------------------------------- */
        var from  = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to    = from.AddYears(1);
        const int seed = 11;

        var hours = MockedDataGenerators.GenerateMockConsumption(seed);
        Consumption
            .GetAverageHourlyConsumption(Arg.Any<OrganizationId>(), from, to, Arg.Any<CancellationToken>())
            .Returns(hours);

        var strict = MockedDataGenerators.GenerateMockClaims(seed, from, to, strictHourlyOnly: true);
        var all    = MockedDataGenerators.GenerateMockClaims(seed, from, to, strictHourlyOnly: false);

        Wallet.GetClaims(Arg.Any<Guid>(), from, to, TimeMatch.Hourly, Arg.Any<CancellationToken>())
              .Returns(new ResultList<Claim> { Result = strict,                 Metadata = new PageInfo { Count = strict.Count, Offset = 0, Limit = strict.Count, Total = strict.Count }
              });

        Wallet.GetClaims(Arg.Any<Guid>(), from, to, TimeMatch.All, Arg.Any<CancellationToken>())
              .Returns(new ResultList<Claim> { Result = all,                 Metadata = new PageInfo { Count = all.Count, Offset = 0, Limit = all.Count, Total = all.Count }
              });

        Factory = new TransferAgreementsApiWebApplicationFactory
        {
            ConnectionString = Pg.GetConnectionString(),
            PdfUrl           = pdfEndpoint
        };

        Factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                services.RemoveAll<IWalletClient>();
                services.RemoveAll<API.Transfer.Api.Services.IConsumptionService>();
                services.AddSingleton(Wallet);
                services.AddSingleton(Consumption);
            });
        });

        Factory.Start();
    }

    /* ---------------- teardown ---------------------------- */
    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Pg.DisposeAsync();
        await Pdf.DisposeAsync();
    }
}
