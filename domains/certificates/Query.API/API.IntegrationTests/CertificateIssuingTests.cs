using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Extensions;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using API.MeasurementsSyncer;
using API.Models;
using API.UnitTests;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V3;
using EnergyOrigin.WalletClient.Models;
using EnergyTrackAndTrace.Testing.Attributes;
using EnergyTrackAndTrace.Testing.Extensions;
using FluentAssertions;
using Meteringpoint.V1;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class CertificateIssuingTests : TestBase
{
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly MeasurementsWireMock _measurementsWireMock;

    public CertificateIssuingTests(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        _integrationTestFixture = integrationTestFixture;
        _measurementsWireMock = integrationTestFixture.MeasurementsMock;
    }

    [Fact]
    [E2ETest]
    public async Task MeasurementsSyncerSendsMeasurementsToStamp_ExpectInWallet()
    {
        var gsrn = Any.Gsrn();
        await AddGsrnToSponsoredTableAsync(gsrn);
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

        var measurementClientMock = Substitute.For<IMeasurementClient>();
        measurementClientMock.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(
            EnergyTrackAndTrace.Testing.Any.DH3MeasurementsApiResponse(gsrn, [EnergyTrackAndTrace.Testing.Any.PointAggregation(utcMidnight.ToUnixTimeSeconds(), 42)]));
        factory.MeasurementClient = measurementClientMock;

        var dataHubFacadeClientMock = Substitute.For<IDataHubFacadeClient>();
        dataHubFacadeClientMock
            .ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(
                new ListMeteringPointForCustomerCaResponse
                {
                    Relations =
                    [
                        new()
                        {
                            MeteringPointId = gsrn.Value,
                            ValidFromDate = DateTime.Now.AddHours(-1)
                        }
                    ],
                    Rejections = []
                });
        factory.DataHubFacadeClient = dataHubFacadeClientMock;

        var meteringpointClientMock = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        var meteringPoint = EnergyTrackAndTrace.Testing.Any.MeteringPoint(gsrn);
        var mockedMeteringPointsResponse = new MeteringPointsResponse
        {
            MeteringPoints = { meteringPoint }
        };

        meteringpointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(mockedMeteringPointsResponse);
        factory.MeteringpointClient = meteringpointClientMock;

        var client = await factory.CreateWallet(orgId);

        factory.Start();

        await factory.AddContract(subject, orgId, gsrn.Value, utcMidnight, MeteringPointType.Production, _measurementsWireMock);
        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(res => res.Any(), orgId);

        queryResponse.Should().HaveCount(1);
        var granularCertificate = queryResponse.Single();

        granularCertificate.Start.Should().Be(utcMidnight.ToUnixTimeSeconds());
        granularCertificate.End.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42m.ToWattHours());
        granularCertificate.CertificateType.Should().Be(CertificateType.Production);

        granularCertificate.Attributes.Should().NotBeEmpty();
        var address = new Address(meteringPoint.StreetName, meteringPoint.BuildingNumber, meteringPoint.FloorId, meteringPoint.RoomId, meteringPoint.Postcode, meteringPoint.CityName, "Danmark", meteringPoint.MunicipalityCode, meteringPoint.CitySubDivisionName);
        granularCertificate.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { EnergyTagAttributeKeys.EnergyTagGcIssuer, "Energinet" },
            { EnergyTagAttributeKeys.EnergyTagGcIssueMarketZone, "DK1" },
            { EnergyTagAttributeKeys.EnergyTagCountry, "Denmark" },
            { EnergyTagAttributeKeys.EnergyTagGcIssuanceDateStamp, DateTimeOffset.Now.ToString("d") },
            { EnergyTagAttributeKeys.EnergyTagProductionStartingIntervalTimestamp, granularCertificate.Start.ToString() },
            { EnergyTagAttributeKeys.EnergyTagProductionEndingIntervalTimestamp, granularCertificate.End.ToString() },
            { EnergyTagAttributeKeys.EnergyTagGcFaceValue, granularCertificate.Quantity.ToString() },
            { EnergyTagAttributeKeys.EnergyTagProductionDeviceUniqueIdentification, gsrn.Value },
            { EnergyTagAttributeKeys.EnergyTagProducedEnergySource, "F01040100" },
            { EnergyTagAttributeKeys.EnergyTagProducedEnergyTechnology, "T010000" },
            { EnergyTagAttributeKeys.EnergyTagConnectedGridIdentification, meteringPoint.MeteringGridAreaId },
            { EnergyTagAttributeKeys.EnergyTagProductionDeviceLocation, address.ToString() },
            { EnergyTagAttributeKeys.EnergyTagProductionDeviceCapacity, meteringPoint.Capacity },
            { EnergyTagAttributeKeys.EnergyTagProductionDeviceCommercialOperationDate, "N/A" },
            { EnergyTagAttributeKeys.EnergyTagEnergyCarrier, "Electricity" },
            { EnergyTagAttributeKeys.EnergyTagGcIssueDeviceType, "Production" },
            { EnergyTagAttributeKeys.EnergyTagDisclosure, "True" },
            { "municipality_code", meteringPoint.MunicipalityCode },
            { EnergyTagAttributeKeys.EnergyTagSponsored, "True" },
            { "isTrial", "False" }
        });
    }

    private async Task AddGsrnToSponsoredTableAsync(Gsrn gsrn)
    {
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_integrationTestFixture.PostgresContainer.ConnectionString)
                .Options);
        db.Sponsorships.Add(new Sponsorship
        {
            SponsorshipGSRN = gsrn,
            SponsorshipEndDate = DateTimeOffset.MaxValue
        });
        await db.SaveChangesAsync();
    }
}
