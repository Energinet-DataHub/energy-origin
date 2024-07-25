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
using NSubstitute;
using ProjectOriginClients.Models;
using Testing.Helpers;
using Xunit;
using Measurements.V1;
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
        factory.Start();

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Production, measurementsWireMock);

        var client = factory.CreateWalletClient(subject);

        var queryResponse = await client.RepeatedlyQueryCertificatesUntil(res => res.Any());

        queryResponse.Should().HaveCount(1);
        var granularCertificate = queryResponse.Single();

        granularCertificate.Start.Should().Be(utcMidnight.ToUnixTimeSeconds());
        granularCertificate.End.Should().Be(utcMidnight.AddHours(1).ToUnixTimeSeconds());
        granularCertificate.GridArea.Should().Be("DK1");
        granularCertificate.Quantity.Should().Be(42);
        granularCertificate.CertificateType.Should().Be(CertificateType.Production);

        //TODO ask Martin Schmidt about hashing the assetId
        granularCertificate.Attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { "assetId", gsrn },
            { "fuelCode", "F01040100" },
            { "techCode", "T010000" }
        });
    }
}
