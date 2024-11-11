using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Extensions;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using API.MeasurementsSyncer;
using API.UnitTests;
using DataContext.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V2;
using FluentAssertions;
using Measurements.V1;
using Meteringpoint.V1;
using NSubstitute;
using ProjectOriginClients.Models;
using Testing.Extensions;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class CertificateIssuingTests : TestBase
{
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly MeasurementsWireMock _measurementsWireMock;

    public CertificateIssuingTests(IntegrationTestFixture integrationTestFixture)
    {
        _integrationTestFixture = integrationTestFixture;
        _measurementsWireMock = integrationTestFixture.MeasurementsMock;
    }

    [Fact]
    public async Task MeasurementsSyncerSendsMeasurementsToStamp_ExpectInWallet()
    {
        var gsrn = Any.Gsrn();
        var subject = Guid.NewGuid().ToString();
        var orgId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var factory = new QueryApiWebApplicationFactory();
        factory.ConnectionString = _integrationTestFixture.WebApplicationFactory.ConnectionString;
        factory.RabbitMqOptions = _integrationTestFixture.WebApplicationFactory.RabbitMqOptions;
        factory.MeasurementsUrl = _integrationTestFixture.WebApplicationFactory.MeasurementsUrl;
        factory.WalletUrl = _integrationTestFixture.WebApplicationFactory.WalletUrl;
        factory.StampUrl = _integrationTestFixture.WebApplicationFactory.StampUrl;
        factory.RegistryName = _integrationTestFixture.WebApplicationFactory.RegistryName;
        factory.MeasurementsSyncEnabled = true;

        var measurementClientMock = Substitute.For<Measurements.V1.Measurements.MeasurementsClient>();
        measurementClientMock.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(
            new GetMeasurementsResponse
            {
                Measurements =
                {
                    new Measurement()
                    {
                        Gsrn = gsrn.Value,
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
            MeteringPointId = gsrn.Value,
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

        await factory.AddContract(subject, orgId, gsrn.Value, utcMidnight, MeteringPointType.Production, _measurementsWireMock);

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
        var address = new Address(meteringPoint.StreetName, meteringPoint.BuildingNumber, meteringPoint.CityName, meteringPoint.Postcode, "Denmark");
        granularCertificate.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { EnergyTagAttributeKeys.EnergyTagGcIssuer, "Energinet" },
            { EnergyTagAttributeKeys.EnergyTagGcIssueMarketZone, granularCertificate.GridArea },
            { EnergyTagAttributeKeys.EnergyTagCountry, "Denmark" },
            { EnergyTagAttributeKeys.EnergyTagGcIssuanceDateStamp, DateTimeOffset.Now.ToString("d") },
            { EnergyTagAttributeKeys.EnergyTagProductionStartingIntervalTimestamp, granularCertificate.Start.ToString() },
            { EnergyTagAttributeKeys.EnergyTagProductionEndingIntervalTimestamp, granularCertificate.End.ToString() },
            { EnergyTagAttributeKeys.EnergyTagGcFaceValue, granularCertificate.Quantity.ToString() },
            { EnergyTagAttributeKeys.EnergyTagProductionDeviceUniqueIdentification, gsrn.Value },
            { EnergyTagAttributeKeys.EnergyTagProducedEnergySource, "F01040100" },
            { EnergyTagAttributeKeys.EnergyTagProducedEnergyTechnology, "T010000" },
            { EnergyTagAttributeKeys.EnergyTagConnectedGridIdentification, granularCertificate.GridArea },
            { EnergyTagAttributeKeys.EnergyTagProductionDeviceLocation, address.ToString() },
            { EnergyTagAttributeKeys.EnergyTagProductionDeviceCapacity, meteringPoint.Capacity },
            { EnergyTagAttributeKeys.EnergyTagProductionDeviceCommercialOperationDate, "N/A" },
            { EnergyTagAttributeKeys.EnergyTagEnergyCarrier, "Electricity" },
            { EnergyTagAttributeKeys.EnergyTagGcIssueDeviceType, "Production" }
        });
    }
}
