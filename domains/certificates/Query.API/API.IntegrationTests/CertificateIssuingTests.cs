using System;
using System.Linq;
using System.Threading.Tasks;
using API.IntegrationTests.Extensions;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using DataContext.ValueObjects;
using FluentAssertions;
using MassTransit;
using MeasurementEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectOriginClients.Models;
using Testing.Helpers;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class CertificateIssuingTests : TestBase
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly MeasurementsWireMock measurementsWireMock;

    public CertificateIssuingTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.WebApplicationFactory;
        measurementsWireMock = integrationTestFixture.MeasurementsMock;
    }

    [Fact]
    public async Task GetList_NoCertificates_ReturnsNoContent()
    {
        var subject = Guid.NewGuid().ToString();

        var client = factory.CreateWalletClient(subject);

        var queryResponse = await client.QueryCertificates();

        queryResponse.Should().BeEmpty();
    }

    [Fact]
    public async Task GetList_MeasurementFromProductionMeteringPointAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Production, measurementsWireMock);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().Publish(measurement);

        var client = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(res => res.Any());

        queryResponse.Should().HaveCount(1);
        var granularCertificate = queryResponse.Single();

        granularCertificate.Start.Should().Be(utcMidnight.ToUnixTimeSeconds());
        granularCertificate.End.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.CertificateType.Should().Be(CertificateType.Production);
        granularCertificate.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn },
            { "fuelCode", "F01040100" },
            { "techCode", "T010000" }
        });
    }

    [Fact]
    public async Task GetList_MeasurementFromConsumptionMeteringPointAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Consumption, measurementsWireMock);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().Publish(measurement);

        var client = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(res => res.Any());

        queryResponse.Should().HaveCount(1);
        var granularCertificate = queryResponse.Single();

        granularCertificate.Start.Should().Be(utcMidnight.ToUnixTimeSeconds());
        granularCertificate.End.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.CertificateType.Should().Be(CertificateType.Consumption);
        granularCertificate.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn }
        });
    }

    [Fact]
    public async Task GetList_SameMeasurementFromProductionMeteringPointAddedTwice_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Production, measurementsWireMock);

        var dateFrom = utcMidnight.ToUnixTimeSeconds();
        var dateTo = utcMidnight.AddHours(1).ToUnixTimeSeconds();

        var measurement1 = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: dateFrom,
            DateTo: dateTo,
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        var measurement2 = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: dateFrom,
            DateTo: dateTo,
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().PublishBatch(new[] { measurement1, measurement2 });

        var client = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(res => res.Any());

        queryResponse.Should().HaveCount(1);
        var granularCertificate = queryResponse.Single();

        granularCertificate.Start.Should().Be(utcMidnight.ToUnixTimeSeconds());
        granularCertificate.End.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.CertificateType.Should().Be(CertificateType.Production);
        granularCertificate.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn },
            { "fuelCode", "F01040100" },
            { "techCode", "T010000" }
        });
    }

    [Fact]
    public async Task GetList_SameMeasurementFromConsumptionMeteringPointAddedTwice_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Consumption, measurementsWireMock);

        var dateFrom = utcMidnight.ToUnixTimeSeconds();
        var dateTo = utcMidnight.AddHours(1).ToUnixTimeSeconds();

        var measurement1 = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: dateFrom,
            DateTo: dateTo,
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        var measurement2 = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: dateFrom,
            DateTo: dateTo,
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().PublishBatch(new[] { measurement1, measurement2 });

        var client = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(res => res.Any());

        queryResponse.Should().HaveCount(1);
        var granularCertificate = queryResponse.Single();

        granularCertificate.Start.Should().Be(utcMidnight.ToUnixTimeSeconds());
        granularCertificate.End.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.CertificateType.Should().Be(CertificateType.Consumption);
        granularCertificate.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn }
        });
    }

    [Fact]
    public async Task GetList_FiveMeasurementsFromProductionMeteringPointAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Production, measurementsWireMock);

        const int measurementCount = 5;

        var measurements = Enumerable.Range(0, measurementCount)
            .Select(i => new EnergyMeasuredIntegrationEvent(
                GSRN: gsrn,
                DateFrom: utcMidnight.AddHours(i).ToUnixTimeSeconds(),
                DateTo: utcMidnight.AddHours(i + 1).ToUnixTimeSeconds(),
                Quantity: 42 + i,
                Quality: MeasurementQuality.Measured))
            .ToArray();

        await factory.GetMassTransitBus().PublishBatch(measurements);

        var client = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(res => res.Count() == measurementCount);

        var granularCertificates = queryResponse.OrderBy(gc => gc.Start).ToArray();

        granularCertificates[0].Start.Should().Be(utcMidnight.ToUnixTimeSeconds());
        granularCertificates[0].End.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificates[0].GridArea.Should().Be("DK1");
        granularCertificates[0].Quantity.Should().Be(42);
        granularCertificates[0].CertificateType.Should().Be(CertificateType.Production);
        granularCertificates[0].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn },
            { "fuelCode", "F01040100" },
            { "techCode", "T010000" }
        });

        granularCertificates[1].Start.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificates[1].End.Should().Be(utcMidnight.AddHours(2).ToUnixTimeSeconds());
        granularCertificates[1].GridArea.Should().Be("DK1");
        granularCertificates[1].Quantity.Should().Be(43);
        granularCertificates[1].CertificateType.Should().Be(CertificateType.Production);
        granularCertificates[1].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn },
            { "fuelCode", "F01040100" },
            { "techCode", "T010000" }
        });

        granularCertificates[2].Start.Should().Be(utcMidnight.AddHours(2).ToUnixTimeSeconds());
        granularCertificates[2].End.Should().Be(utcMidnight.AddHours(3).ToUnixTimeSeconds());
        granularCertificates[2].GridArea.Should().Be("DK1");
        granularCertificates[2].Quantity.Should().Be(44);
        granularCertificates[2].CertificateType.Should().Be(CertificateType.Production);
        granularCertificates[2].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn },
            { "fuelCode", "F01040100" },
            { "techCode", "T010000" }
        });

        granularCertificates[3].Start.Should().Be(utcMidnight.AddHours(3).ToUnixTimeSeconds());
        granularCertificates[3].End.Should().Be(utcMidnight.AddHours(4).ToUnixTimeSeconds());
        granularCertificates[3].GridArea.Should().Be("DK1");
        granularCertificates[3].Quantity.Should().Be(45);
        granularCertificates[3].CertificateType.Should().Be(CertificateType.Production);
        granularCertificates[3].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn },
            { "fuelCode", "F01040100" },
            { "techCode", "T010000" }
        });

        granularCertificates[4].Start.Should().Be(utcMidnight.AddHours(4).ToUnixTimeSeconds());
        granularCertificates[4].End.Should().Be(utcMidnight.AddHours(5).ToUnixTimeSeconds());
        granularCertificates[4].GridArea.Should().Be("DK1");
        granularCertificates[4].Quantity.Should().Be(46);
        granularCertificates[4].CertificateType.Should().Be(CertificateType.Production);
        granularCertificates[4].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn },
            { "fuelCode", "F01040100" },
            { "techCode", "T010000" }
        });
    }


    [Fact]
    public async Task GetList_FiveMeasurementsFromConsumptionMeteringPointAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Consumption, measurementsWireMock);

        const int measurementCount = 5;

        var measurements = Enumerable.Range(0, measurementCount)
            .Select(i => new EnergyMeasuredIntegrationEvent(
                GSRN: gsrn,
                DateFrom: utcMidnight.AddHours(i).ToUnixTimeSeconds(),
                DateTo: utcMidnight.AddHours(i + 1).ToUnixTimeSeconds(),
                Quantity: 42 + i,
                Quality: MeasurementQuality.Measured))
            .ToArray();

        await factory.GetMassTransitBus().PublishBatch(measurements);

        var client = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(res => res.Count() == measurementCount);

        var granularCertificates = queryResponse.OrderBy(gc => gc.Start).ToArray();

        granularCertificates[0].Start.Should().Be(utcMidnight.AddHours(0).ToUnixTimeSeconds());
        granularCertificates[0].End.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificates[0].GridArea.Should().Be("DK1");
        granularCertificates[0].Quantity.Should().Be(42);
        granularCertificates[0].CertificateType.Should().Be(CertificateType.Consumption);
        granularCertificates[0].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn }
        });

        granularCertificates[1].Start.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificates[1].End.Should().Be(utcMidnight.AddHours(2).ToUnixTimeSeconds());
        granularCertificates[1].GridArea.Should().Be("DK1");
        granularCertificates[1].Quantity.Should().Be(43);
        granularCertificates[1].CertificateType.Should().Be(CertificateType.Consumption);
        granularCertificates[1].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn }
        });

        granularCertificates[2].Start.Should().Be(utcMidnight.AddHours(2).ToUnixTimeSeconds());
        granularCertificates[2].End.Should().Be(utcMidnight.AddHours(3).ToUnixTimeSeconds());
        granularCertificates[2].GridArea.Should().Be("DK1");
        granularCertificates[2].Quantity.Should().Be(44);
        granularCertificates[2].CertificateType.Should().Be(CertificateType.Consumption);
        granularCertificates[2].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn }
        });

        granularCertificates[3].Start.Should().Be(utcMidnight.AddHours(3).ToUnixTimeSeconds());
        granularCertificates[3].End.Should().Be(utcMidnight.AddHours(4).ToUnixTimeSeconds());
        granularCertificates[3].GridArea.Should().Be("DK1");
        granularCertificates[3].Quantity.Should().Be(45);
        granularCertificates[3].CertificateType.Should().Be(CertificateType.Consumption);
        granularCertificates[3].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn }
        });

        granularCertificates[4].Start.Should().Be(utcMidnight.AddHours(4).ToUnixTimeSeconds());
        granularCertificates[4].End.Should().Be(utcMidnight.AddHours(5).ToUnixTimeSeconds());
        granularCertificates[4].GridArea.Should().Be("DK1");
        granularCertificates[4].Quantity.Should().Be(46);
        granularCertificates[4].CertificateType.Should().Be(CertificateType.Consumption);
        granularCertificates[4].Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn }
        });
    }
}
