using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Extensions;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Helpers;
using API.IntegrationTests.Mocks;
using API.IntegrationTests.Testcontainers;
using API.Query.API.ApiModels.Responses;
using FluentAssertions;
using FluentAssertions.Equivalency;
using MassTransit;
using MeasurementEvents;
using Xunit;

namespace API.IntegrationTests;

public sealed class CertificateIssuingTests :
    TestBase,
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<MartenDbContainer>,
    IClassFixture<RabbitMqContainer>,
    IClassFixture<DataSyncWireMock>,
    IClassFixture<RegistryConnectorApplicationFactory>,
    IClassFixture<ProjectOriginContainer>
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;
    private readonly RegistryConnectorApplicationFactory registryConnectorFactory;

    public CertificateIssuingTests(
        QueryApiWebApplicationFactory factory,
        MartenDbContainer martenDbContainer,
        RabbitMqContainer rabbitMqContainer,
        DataSyncWireMock dataSyncWireMock,
        RegistryConnectorApplicationFactory registryConnectorFactory,
        ProjectOriginContainer poContainer)
    {
        this.dataSyncWireMock = dataSyncWireMock;
        this.registryConnectorFactory = registryConnectorFactory;
        this.factory = factory;
        this.factory.MartenConnectionString = martenDbContainer.ConnectionString;
        this.factory.DataSyncUrl = dataSyncWireMock.Url;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
        this.registryConnectorFactory.RabbitMqOptions = rabbitMqContainer.Options;
        this.registryConnectorFactory.RegistryOptions = poContainer.Options;

        var regConClient = registryConnectorFactory.CreateClient();
    }

    [Fact]
    public async Task GetList_UnauthenticatedUser_ReturnsUnauthorized()
    {
        using var client = factory.CreateUnauthenticatedClient();
        using var certificatesResponse = await client.GetAsync("api/certificates");

        Assert.Equal(HttpStatusCode.Unauthorized, certificatesResponse.StatusCode);
    }

    [Fact]
    public async Task GetList_NoCertificates_ReturnsNoContent()
    {
        var subject = Guid.NewGuid().ToString();

        using var client = factory.CreateAuthenticatedClient(subject);

        using var response = await client.GetAsync("api/certificates");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetList_MeasurementAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, dataSyncWireMock);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().Publish(measurement);

        using var client = factory.CreateAuthenticatedClient(subject);

        var certificateList =
            await client.RepeatedlyGetUntil<CertificateList>("api/certificates", res => res.Result.Any());

        var expected = new CertificateList
        {
            Result = new[]
            {
                new Certificate
                {
                    DateFrom = utcMidnight.ToUnixTimeSeconds(),
                    DateTo = utcMidnight.AddHours(1).ToUnixTimeSeconds(),
                    Quantity = 42,
                    FuelCode = "F00000000",
                    TechCode = "T070000",
                    GridArea = "DK1",
                    GSRN = gsrn
                }
            }
        };

        certificateList.Should().BeEquivalentTo(expected, CertificateListAssertionOptions);
    }

#pragma warning disable xUnit1004
    [Fact(Skip = "TNE 15/05/2023: Skipped until concurrency issues has been fixed.")]
#pragma warning restore xUnit1004
    public async Task GetList_FiveMeasurementAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, dataSyncWireMock);

        var measurements = Enumerable.Range(0, 5)
            .Select(i => new EnergyMeasuredIntegrationEvent(
                GSRN: gsrn,
                DateFrom: utcMidnight.AddHours(i).ToUnixTimeSeconds(),
                DateTo: utcMidnight.AddHours(i + 1).ToUnixTimeSeconds(),
                Quantity: 42 + i,
                Quality: MeasurementQuality.Measured))
            .ToArray();

        await factory.GetMassTransitBus().PublishBatch(measurements);

        using var client = factory.CreateAuthenticatedClient(subject);

        var certificateList =
            await client.RepeatedlyGetUntil<CertificateList>("api/certificates", res => res.Result.Count() == 5);

        var expected = new CertificateList
        {
            Result = new[]
            {
                new Certificate
                {
                    Quantity = 46,
                    DateFrom = utcMidnight.AddHours(4).ToUnixTimeSeconds(),
                    DateTo = utcMidnight.AddHours(5).ToUnixTimeSeconds(),
                    GridArea = "DK1",
                    GSRN = gsrn,
                    FuelCode = "F00000000",
                    TechCode = "T070000"
                },
                new Certificate
                {
                    Quantity = 45,
                    DateFrom = utcMidnight.AddHours(3).ToUnixTimeSeconds(),
                    DateTo = utcMidnight.AddHours(4).ToUnixTimeSeconds(),
                    GridArea = "DK1",
                    GSRN = gsrn,
                    FuelCode = "F00000000",
                    TechCode = "T070000"
                },
                new Certificate
                {
                    Quantity = 44,
                    DateFrom = utcMidnight.AddHours(2).ToUnixTimeSeconds(),
                    DateTo = utcMidnight.AddHours(3).ToUnixTimeSeconds(),
                    GridArea = "DK1",
                    GSRN = gsrn,
                    FuelCode = "F00000000",
                    TechCode = "T070000"
                },
                new Certificate
                {
                    Quantity = 43,
                    DateFrom = utcMidnight.AddHours(1).ToUnixTimeSeconds(),
                    DateTo = utcMidnight.AddHours(2).ToUnixTimeSeconds(),
                    GridArea = "DK1",
                    GSRN = gsrn,
                    FuelCode = "F00000000",
                    TechCode = "T070000"
                },
                new Certificate
                {
                    Quantity = 42,
                    DateFrom = utcMidnight.ToUnixTimeSeconds(),
                    DateTo = utcMidnight.AddHours(1).ToUnixTimeSeconds(),
                    GridArea = "DK1",
                    GSRN = gsrn,
                    FuelCode = "F00000000",
                    TechCode = "T070000"
                }
            }
        };

        certificateList.Should().BeEquivalentTo(expected, CertificateListAssertionOptions);
    }

    private static EquivalencyAssertionOptions<CertificateList> CertificateListAssertionOptions(
        EquivalencyAssertionOptions<CertificateList> options) =>
        options
            .WithStrictOrderingFor(l => l.Result)
            .For(l => l.Result).Exclude(c => c.Id);
}
