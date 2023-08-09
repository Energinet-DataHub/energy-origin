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

[Collection(nameof(ProjectOriginStackCollection))]
public sealed class CertificateIssuingTests :
    TestBase,
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<MartenDbContainer>,
    IClassFixture<RabbitMqContainer>,
    IClassFixture<DataSyncWireMock>,
    IClassFixture<RegistryConnectorApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;

    public CertificateIssuingTests(
        QueryApiWebApplicationFactory factory,
        MartenDbContainer martenDbContainer,
        RabbitMqContainer rabbitMqContainer,
        DataSyncWireMock dataSyncWireMock,
        RegistryConnectorApplicationFactory registryConnectorFactory,
        ProjectOriginStack projectOriginStack)
    {
        this.dataSyncWireMock = dataSyncWireMock;
        this.factory = factory;
        this.factory.MartenConnectionString = martenDbContainer.ConnectionString;
        this.factory.DataSyncUrl = dataSyncWireMock.Url;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
        registryConnectorFactory.RabbitMqOptions = rabbitMqContainer.Options;
        registryConnectorFactory.ProjectOriginOptions = projectOriginStack.Options;
        registryConnectorFactory.Start();
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

    [Fact(Skip = "Takes too long time due to registry")]
    public async Task GetList_FiveMeasurementAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, dataSyncWireMock);

        const int cnt = 24;

        var measurements = Enumerable.Range(0, cnt)
            .Select(i => new EnergyMeasuredIntegrationEvent(
                GSRN: gsrn,
                DateFrom: utcMidnight.AddHours(i).ToUnixTimeSeconds(),
                DateTo: utcMidnight.AddHours(i + 1).ToUnixTimeSeconds(),
                Quantity: 42 + i,
                Quality: MeasurementQuality.Measured))
            .ToArray();

        await factory.GetMassTransitBus().PublishBatch(measurements);

        using var client = factory.CreateAuthenticatedClient(subject);

        //await Task.Delay(TimeSpan.FromMinutes(5));

        var certificateList =
            await client.RepeatedlyGetUntil<CertificateList>("api/certificates",
                res => res.Result.Count() == cnt,
                timeLimit: TimeSpan.FromMinutes(10));

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
