using System;
using System.Linq;
using System.Threading.Tasks;
using API.IntegrationTests.Extensions;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using DataContext.ValueObjects;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading;
using API.MeasurementsSyncer;
using NSubstitute;
using ProjectOriginClients.Models;
using Testing.Helpers;
using Xunit;
using Measurements.V1;
using Meteringpoint.V1;
using Testing.Extensions;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class CertificateIssuingTests : TestBase
{
    private readonly IntegrationTestFixture integrationTestFixture;
    private readonly MeasurementsWireMock measurementsWireMock;

    public CertificateIssuingTests(IntegrationTestFixture integrationTestFixture)
    {
        this.integrationTestFixture = integrationTestFixture;
        measurementsWireMock = integrationTestFixture.MeasurementsMock;
    }

    [Fact]
    public async Task MeasurementsSyncerSendsMeasurementsToStamp_ExpectInWallet()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var subject = Guid.NewGuid().ToString();
        var orgId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var factory = new QueryApiWebApplicationFactory();
        factory.ConnectionString = integrationTestFixture.WebApplicationFactory.ConnectionString;
        factory.RabbitMqOptions = integrationTestFixture.WebApplicationFactory.RabbitMqOptions;
        factory.MeasurementsUrl = integrationTestFixture.WebApplicationFactory.MeasurementsUrl;
        factory.WalletUrl = integrationTestFixture.WebApplicationFactory.WalletUrl;
        factory.StampUrl = integrationTestFixture.WebApplicationFactory.StampUrl;
        factory.RegistryName = integrationTestFixture.WebApplicationFactory.RegistryName;
        factory.MeasurementsSyncEnabled = true;

        var measurementClientMock = Substitute.For<Measurements.V1.Measurements.MeasurementsClient>();
        measurementClientMock.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(
            new GetMeasurementsResponse
            {
                Measurements =
                {
                    new Measurement()
                    {
                        Gsrn = gsrn,
                        Quantity = 42,
                        DateFrom = utcMidnight.ToUnixTimeSeconds(),
                        DateTo = utcMidnight.AddHours(1).ToUnixTimeSeconds(),
                        Quality = EnergyQuantityValueQuality.Measured,
                        QuantityMissing = false
                    }
                }
            });

        factory.measurementsClient = measurementClientMock;
        var meteringpointClientMock = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        var meteringPoint = new MeteringPoint
        {
            MeteringPointId = gsrn,
            MeteringPointAlias = "alias",
            ConsumerStartDate = "consumerStartDate",
            Capacity = "123",
            BuildingNumber = "buildingNumber",
            CityName = "cityName",
            Postcode = "postcode",
            StreetName = "streetName",
        };
        var mockedMeteringPointsResponse = new MeteringPointsResponse
        {
            MeteringPoints = { meteringPoint }

        };

        meteringpointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(mockedMeteringPointsResponse);
        factory.MeteringpointClient = meteringpointClientMock;

        factory.Start();

        await factory.AddContract(subject, orgId, gsrn, utcMidnight, MeteringPointType.Production, measurementsWireMock);

        var client = factory.CreateWalletClient(orgId);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(res => res.Any());

        queryResponse.Should().HaveCount(1);
        var granularCertificate = queryResponse.Single();

        granularCertificate.Start.Should().Be(utcMidnight.ToUnixTimeSeconds());
        granularCertificate.End.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.CertificateType.Should().Be(CertificateType.Production);

        granularCertificate.Attributes.Should().NotBeEmpty();
        var address = meteringPoint.BuildingNumber + " " + meteringPoint.StreetName + " " + meteringPoint.CityName + " " + meteringPoint.Postcode;
        granularCertificate.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { AttributeKeys.EnergyTagGcIssuer, "Energinet" },
            { AttributeKeys.EnergyTagGcIssueMarketZone, granularCertificate.GridArea },
            { AttributeKeys.EnergyTagCountry, "Denmark" },
            { AttributeKeys.EnergyTagGcIssuanceDateStamp, DateTimeOffset.Now.ToString("d") },
            { AttributeKeys.EnergyTagProductionStartingIntervalTimestamp, granularCertificate.Start.ToString() },
            { AttributeKeys.EnergyTagProductionEndingIntervalTimestamp, granularCertificate.End.ToString() },
            { AttributeKeys.EnergyTagGcFaceValue, granularCertificate.Quantity.ToString() },
            { AttributeKeys.EnergyTagProductionDeviceUniqueIdentification, gsrn },
            { AttributeKeys.EnergyTagProducedEnergySource, "F01040100" },
            { AttributeKeys.EnergyTagProducedEnergyTechnology, "T010000" },
            { AttributeKeys.EnergyTagConnectedGridIdentification, granularCertificate.GridArea },
            { AttributeKeys.EnergyTagProductionDeviceLocation, address },
            { AttributeKeys.EnergyTagProductionDeviceCapacity, meteringPoint.Capacity },
            { AttributeKeys.EnergyTagProductionDeviceCommercialOperationDate, "N/A" },
            { AttributeKeys.EnergyTagEnergyCarrier, "Electricity" },
            { AttributeKeys.EnergyTagGcIssueDeviceType, "Production" }
        });
    }
}
