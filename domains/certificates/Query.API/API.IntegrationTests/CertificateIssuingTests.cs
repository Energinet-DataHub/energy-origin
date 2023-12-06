using API.IntegrationTests.Extensions;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Helpers;
using API.IntegrationTests.Mocks;
using API.IntegrationTests.Testcontainers;
using DataContext.ValueObjects;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using MassTransit;
using MeasurementEvents;
using ProjectOrigin.WalletSystem.V1;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Attribute = ProjectOrigin.WalletSystem.V1.Attribute;

namespace API.IntegrationTests;

public sealed class CertificateIssuingTests :
    TestBase,
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<PostgresContainer>,
    IClassFixture<RabbitMqContainer>,
    IClassFixture<DataSyncWireMock>,
    IClassFixture<RegistryConnectorApplicationFactory>,
    IClassFixture<ProjectOriginStack>
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;

    public CertificateIssuingTests(
        QueryApiWebApplicationFactory factory,
        PostgresContainer dbContainer,
        RabbitMqContainer rabbitMqContainer,
        DataSyncWireMock dataSyncWireMock,
        RegistryConnectorApplicationFactory registryConnectorFactory,
        ProjectOriginStack projectOriginStack)
    {
        this.dataSyncWireMock = dataSyncWireMock;
        this.factory = factory;
        this.factory.ConnectionString = dbContainer.ConnectionString;
        this.factory.DataSyncUrl = dataSyncWireMock.Url;
        this.factory.WalletUrl = projectOriginStack.WalletUrl;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
        registryConnectorFactory.RabbitMqOptions = rabbitMqContainer.Options;
        registryConnectorFactory.ProjectOriginOptions = projectOriginStack.Options;
        registryConnectorFactory.ConnectionString = dbContainer.ConnectionString;
        registryConnectorFactory.Start();
    }

    [Fact]
    public async Task GetList_NoCertificates_ReturnsNoContent()
    {
        var subject = Guid.NewGuid().ToString();

        var (client, metadata) = factory.CreateWalletClient(subject);

        var queryResponse = await client.QueryGranularCertificatesAsync(new QueryRequest(), metadata);

        queryResponse.GranularCertificates.Should().BeEmpty();
    }

    [Fact]
    public async Task GetList_MeasurementFromProductionMeteringPointAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Production, dataSyncWireMock);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().Publish(measurement);

        var (client, metadata) = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(metadata, res => res.GranularCertificates.Any());

        queryResponse.GranularCertificates.Should().HaveCount(1);
        var granularCertificate = queryResponse.GranularCertificates.Single();

        granularCertificate.Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight));
        granularCertificate.End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(1)));
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.Type.Should().Be(GranularCertificateType.Production);
        granularCertificate.Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute
            {
                Key = "FuelCode",
                Value = "F00000000"
            },
            new Attribute
            {
                Key = "TechCode",
                Value = "T070000"
            },
            new Attribute
            {
                Key = "AssetId",
                Value = gsrn
            }
        });
    }

    [Fact]
    public async Task GetList_MeasurementFromConsumptionMeteringPointAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Consumption, dataSyncWireMock);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().Publish(measurement);

        var (client, metadata) = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(metadata, res => res.GranularCertificates.Any());

        queryResponse.GranularCertificates.Should().HaveCount(1);
        var granularCertificate = queryResponse.GranularCertificates.Single();

        granularCertificate.Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight));
        granularCertificate.End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(1)));
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.Type.Should().Be(GranularCertificateType.Consumption);
        granularCertificate.Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute
            {
                Key = "AssetId",
                Value = gsrn
            }
        });
    }

    [Fact]
    public async Task GetList_SameMeasurementFromProductionMeteringPointAddedTwice_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Production, dataSyncWireMock);

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

        var (client, metadata) = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(metadata, res => res.GranularCertificates.Any());

        queryResponse.GranularCertificates.Should().HaveCount(1);
        var granularCertificate = queryResponse.GranularCertificates.Single();

        granularCertificate.Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight));
        granularCertificate.End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(1)));
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.Type.Should().Be(GranularCertificateType.Production);
        granularCertificate.Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute
            {
                Key = "FuelCode",
                Value = "F00000000"
            },
            new Attribute
            {
                Key = "TechCode",
                Value = "T070000"
            },
            new Attribute
            {
                Key = "AssetId",
                Value = gsrn
            }
        });
    }

    [Fact]
    public async Task GetList_SameMeasurementFromConsumptionMeteringPointAddedTwice_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Consumption, dataSyncWireMock);

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

        var (client, metadata) = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(metadata, res => res.GranularCertificates.Any());

        queryResponse.GranularCertificates.Should().HaveCount(1);
        var granularCertificate = queryResponse.GranularCertificates.Single();

        granularCertificate.Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight));
        granularCertificate.End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(1)));
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.Type.Should().Be(GranularCertificateType.Consumption);
        granularCertificate.Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute
            {
                Key = "AssetId",
                Value = gsrn
            }
        });
    }

    [Fact]
    public async Task GetList_FiveMeasurementsFromProductionMeteringPointAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Production, dataSyncWireMock);

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

        var (client, metadata) = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(metadata, res => res.GranularCertificates.Count == measurementCount);

        var granularCertificates = queryResponse.GranularCertificates.OrderBy(gc => gc.Start).ToArray();

        granularCertificates[0].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(0)));
        granularCertificates[0].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(1)));
        granularCertificates[0].GridArea.Should().Be("DK1");
        granularCertificates[0].Quantity.Should().Be(42);
        granularCertificates[0].Type.Should().Be(GranularCertificateType.Production);
        granularCertificates[0].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "FuelCode", Value = "F00000000" },
            new Attribute { Key = "TechCode", Value = "T070000" },
            new Attribute { Key = "AssetId", Value = gsrn }
        });

        granularCertificates[1].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(1)));
        granularCertificates[1].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(2)));
        granularCertificates[1].GridArea.Should().Be("DK1");
        granularCertificates[1].Quantity.Should().Be(43);
        granularCertificates[1].Type.Should().Be(GranularCertificateType.Production);
        granularCertificates[1].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "FuelCode", Value = "F00000000" },
            new Attribute { Key = "TechCode", Value = "T070000" },
            new Attribute { Key = "AssetId", Value = gsrn }
        });

        granularCertificates[2].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(2)));
        granularCertificates[2].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(3)));
        granularCertificates[2].GridArea.Should().Be("DK1");
        granularCertificates[2].Quantity.Should().Be(44);
        granularCertificates[2].Type.Should().Be(GranularCertificateType.Production);
        granularCertificates[2].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "FuelCode", Value = "F00000000" },
            new Attribute { Key = "TechCode", Value = "T070000" },
            new Attribute { Key = "AssetId", Value = gsrn }
        });

        granularCertificates[3].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(3)));
        granularCertificates[3].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(4)));
        granularCertificates[3].GridArea.Should().Be("DK1");
        granularCertificates[3].Quantity.Should().Be(45);
        granularCertificates[3].Type.Should().Be(GranularCertificateType.Production);
        granularCertificates[3].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "FuelCode", Value = "F00000000" },
            new Attribute { Key = "TechCode", Value = "T070000" },
            new Attribute { Key = "AssetId", Value = gsrn }
        });

        granularCertificates[4].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(4)));
        granularCertificates[4].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(5)));
        granularCertificates[4].GridArea.Should().Be("DK1");
        granularCertificates[4].Quantity.Should().Be(46);
        granularCertificates[4].Type.Should().Be(GranularCertificateType.Production);
        granularCertificates[4].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "FuelCode", Value = "F00000000" },
            new Attribute { Key = "TechCode", Value = "T070000" },
            new Attribute { Key = "AssetId", Value = gsrn }
        });
    }


    [Fact]
    public async Task GetList_FiveMeasurementsFromConsumptionMeteringPointAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Consumption, dataSyncWireMock);

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

        var (client, metadata) = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(metadata, res => res.GranularCertificates.Count == measurementCount);

        var granularCertificates = queryResponse.GranularCertificates.OrderBy(gc => gc.Start).ToArray();

        granularCertificates[0].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(0)));
        granularCertificates[0].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(1)));
        granularCertificates[0].GridArea.Should().Be("DK1");
        granularCertificates[0].Quantity.Should().Be(42);
        granularCertificates[0].Type.Should().Be(GranularCertificateType.Consumption);
        granularCertificates[0].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "AssetId", Value = gsrn }
        });

        granularCertificates[1].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(1)));
        granularCertificates[1].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(2)));
        granularCertificates[1].GridArea.Should().Be("DK1");
        granularCertificates[1].Quantity.Should().Be(43);
        granularCertificates[1].Type.Should().Be(GranularCertificateType.Consumption);
        granularCertificates[1].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "AssetId", Value = gsrn }
        });

        granularCertificates[2].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(2)));
        granularCertificates[2].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(3)));
        granularCertificates[2].GridArea.Should().Be("DK1");
        granularCertificates[2].Quantity.Should().Be(44);
        granularCertificates[2].Type.Should().Be(GranularCertificateType.Consumption);
        granularCertificates[2].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "AssetId", Value = gsrn }
        });

        granularCertificates[3].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(3)));
        granularCertificates[3].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(4)));
        granularCertificates[3].GridArea.Should().Be("DK1");
        granularCertificates[3].Quantity.Should().Be(45);
        granularCertificates[3].Type.Should().Be(GranularCertificateType.Consumption);
        granularCertificates[3].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "AssetId", Value = gsrn }
        });

        granularCertificates[4].Start.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(4)));
        granularCertificates[4].End.Should().Be(Timestamp.FromDateTimeOffset(utcMidnight.AddHours(5)));
        granularCertificates[4].GridArea.Should().Be("DK1");
        granularCertificates[4].Quantity.Should().Be(46);
        granularCertificates[4].Type.Should().Be(GranularCertificateType.Consumption);
        granularCertificates[4].Attributes.Should().BeEquivalentTo(new[]
        {
            new Attribute { Key = "AssetId", Value = gsrn }
        });
    }
}
