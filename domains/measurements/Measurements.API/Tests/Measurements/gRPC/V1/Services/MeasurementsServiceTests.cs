using API;
using System;
using System.Threading.Tasks;
using Measurements.V1;
using Tests.Fixtures;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Linq;
using Metertimeseries.V1;
using Tests.Extensions;
using VerifyXunit;
using FluentAssertions;
using VerifyTests;

namespace Tests.Measurements.gRPC.V1.Services;

[UsesVerify]
public class MeasurementsServiceTests : MeasurementsTestBase
{
    public MeasurementsServiceTests(TestServerFixture<Startup> serverFixture)
        : base(serverFixture)
    {

    }

    [Fact]
    public async Task GetMeasurements()
    {
        var clientMock = Substitute.For<Metertimeseries.V1.MeterTimeSeries.MeterTimeSeriesClient>();

        var mockedResponse = GenerateMeterTimeSeriesResponse();

        clientMock.GetMeterTimeSeriesAsync(Arg.Any<MeterTimeSeriesRequest>())
            .Returns(mockedResponse);
        _serverFixture.ConfigureTestServices += services =>
        {
            var mpClient = services.Single(d => d.ServiceType == typeof(Metertimeseries.V1.MeterTimeSeries.MeterTimeSeriesClient));
            services.Remove(mpClient);
            services.AddSingleton(clientMock);
        };

        var client = new global::Measurements.V1.Measurements.MeasurementsClient(_serverFixture.Channel);
        var request = new GetMeasurementsRequest
        {
            Gsrn = "1234567890123456",
            Actor = Guid.NewGuid().ToString(),
            Subject = Guid.NewGuid().ToString(),
            DateFrom = new DateTimeOffset(2020,1,1,0,0,0,TimeSpan.Zero).ToUnixTimeSeconds(),
            DateTo = new DateTimeOffset(2021, 1, 31, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
        };

        var response = await client.GetMeasurementsAsync(request);

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
    }

    private static MeterTimeSeriesResponse GenerateMeterTimeSeriesResponse()
    {
        var mockedResponse = new MeterTimeSeriesResponse();
        mockedResponse.GetMeterTimeSeriesRejection = null;
        var mp = new MeterTimeSeriesMeteringPoint
        {
            TypeOfMp = "E17",
            ConsumerStartDate = "2017-12-31T23:00:00Z",
            MeteringPointId = "1234567890123456"
        };
        var mpState = new MeteringPointState
        {
            MeterReadingOccurrence = "PT1H",
            SettlementMethod = "E02",
            ValidFromDate = "2020-01-01",
            ValidToDate = "2021-01-31"
        };
        var eQuantity = new NonProfiledEnergyQuantity
        {
            Date = "2021-01-01"
        };
        var qValue = new EnergyQuantityValue
        {
            EnergyQuantity = "0.88",
            EnergyTimeSeriesMeasureUnit = "KWH",
            Position = "1",
            QuantityQuality = "E01"
        };
        eQuantity.EnergyQuantityValues.Add(qValue);
        mpState.NonProfiledEnergyQuantities.Add(eQuantity);
        mp.MeteringPointStates.Add(mpState);
        mockedResponse.GetMeterTimeSeriesResult = new GetMeterTimeSeriesResult
        {
            MeterTimeSeriesMeteringPoint = { mp }
        };
        return mockedResponse;
    }
}
