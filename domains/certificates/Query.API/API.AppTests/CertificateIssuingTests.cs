using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using API.AppTests.Extensions;
using API.AppTests.Infrastructure;
using API.AppTests.Mocks;
using API.Query.API.ApiModels.Responses;
using FluentAssertions;
using FluentAssertions.Equivalency;
using MassTransit;
using MeasurementEvents;
using Xunit;

namespace API.AppTests;

public sealed class CertificateIssuingTests :
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<MartenDbContainer>,
    IClassFixture<RabbitMqContainer>,
    IDisposable
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;

    public CertificateIssuingTests(QueryApiWebApplicationFactory factory, MartenDbContainer martenDbContainer,
        RabbitMqContainer rabbitMqContainer)
    {
        dataSyncWireMock = new DataSyncWireMock(port: 9003);
        this.factory = factory;
        this.factory.MartenConnectionString = martenDbContainer.ConnectionString;
        this.factory.DataSyncUrl = dataSyncWireMock.Url;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
    }

    [Fact(Skip = "I do not want to wait")]
    public async Task GetList_UnauthenticatedUser_ReturnsUnauthorized()
    {
        var client = factory.CreateUnauthenticatedClient();
        var certificatesResponse = await client.GetAsync("api/certificates");

        Assert.Equal(HttpStatusCode.Unauthorized, certificatesResponse.StatusCode);
    }

    [Fact(Skip = "I do not want to wait")]
    public async Task GetList_NoCertificates_ReturnsNoContent()
    {
        var subject = Guid.NewGuid().ToString();

        using var client = factory.CreateAuthenticatedClient(subject);

        var response = await client.GetAsync("api/certificates");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(Skip = "I do not want to wait")]
    public async Task GetList_MeasurementAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = "111111111111111111";

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

    [Fact(Skip = "I do not want to wait")]
    public async Task GetList_FiveMeasurementAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = "222222222222222222";

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

    public void Dispose() => dataSyncWireMock.Dispose();
}
