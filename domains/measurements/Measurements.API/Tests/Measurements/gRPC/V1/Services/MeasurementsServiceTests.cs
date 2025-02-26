using API;
using System;
using System.Collections.Generic;
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
using System.Globalization;
using API.Measurements.Helpers;

namespace Tests.Measurements.gRPC.V1.Services;

public class MeasurementsServiceTests : MeasurementsTestBase, IDisposable
{
    public MeasurementsServiceTests(TestServerFixture<Startup> serverFixture)
        : base(serverFixture)
    {
    }

    public void Dispose()
    {
        _serverFixture.RefreshHostAndGrpcChannelOnNextClient();
    }

    [Fact]
    public async Task GetMeasurements()
    {
        var dateFrom = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var dateTo = new DateTimeOffset(2021, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var clientMock = Substitute.For<Metertimeseries.V1.MeterTimeSeries.MeterTimeSeriesClient>();

        var mockedResponse = GenerateMeterTimeSeriesResponse(dateOfReading: dateFrom.AddDays(1).ToString("yyyy-MM-dd"));

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
            DateFrom = dateFrom.ToUnixTimeSeconds(),
            DateTo = dateTo.ToUnixTimeSeconds(),
        };

        var response = await client.GetMeasurementsAsync(request);

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
        var quality = mockedResponse.GetMeterTimeSeriesResult.MeterTimeSeriesMeteringPoint.First().MeteringPointStates.First()
            .NonProfiledEnergyQuantities.First().EnergyQuantityValues.First().QuantityQuality;
        response.Measurements.First().Quality.Should().Be(MeterTimeSeriesHelper.GetQuantityQualityFromMeterReading(quality));
    }

    [Fact]
    public async Task GetMeasurements_SelectsFourHourInterval_ReturnsFourMeasurements()
    {
        var dateFrom = DateTimeOffset.Parse("2022-01-01T10:00:00+01:00");
        var dateTo = DateTimeOffset.Parse("2022-01-01T14:00:00+01:00");

        var clientMock = Substitute.For<Metertimeseries.V1.MeterTimeSeries.MeterTimeSeriesClient>();

        double quantity = 1856.88;
        var quantities = Enumerable.Range(1, 24).Select(i => new EnergyQuantityValue()
        {
            Position = i.ToString(),
            EnergyQuantity = quantity.ToString(CultureInfo.InvariantCulture),
            EnergyTimeSeriesMeasureUnit = "KWH",
            QuantityQuality = "E01",
            QuantityMissingIndicator = "false"
        }).ToArray();

        var mockedResponse = GenerateMeterTimeSeriesResponse(quantities: quantities, dateOfReading: dateFrom.ToString("yyyy-MM-dd"));

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
            Gsrn = mockedResponse.GetMeterTimeSeriesResult.MeterTimeSeriesMeteringPoint.First().MeteringPointId,
            Actor = Guid.NewGuid().ToString(),
            Subject = Guid.NewGuid().ToString(),
            DateFrom = dateFrom.ToUnixTimeSeconds(),
            DateTo = dateTo.ToUnixTimeSeconds(),
        };

        var response = await client.GetMeasurementsAsync(request);

        response.Should().NotBeNull();
        response.Measurements.Count.Should().Be(4);

        var item = response.Measurements.First();
        item.Should().NotBeNull();
        item.DateFrom.Should().Be(dateFrom.ToUnixTimeSeconds());
        item.DateTo.Should().Be(dateFrom.AddHours(1).ToUnixTimeSeconds());

        var last = response.Measurements.Last();
        last.Should().NotBeNull();
        last.DateFrom.Should().Be(dateTo.AddHours(-1).ToUnixTimeSeconds());
        last.DateTo.Should().Be(dateTo.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetMeasurements_WithDifferentMeterReadingOccurrences_ReturnsListOfMeteringPointData()
    {
        var occurenceOne = "PT1H";
        var occurenceTwo = "PT15M";

        var dateFrom = DateTimeOffset.Parse("2022-01-01T00:00:00+01:00");
        var dateTo = DateTimeOffset.Parse("2022-01-03T00:00:00+01:00");

        Random r = new Random();

        var hourlyValues = Enumerable.Range(0, 24).Select(i => r.NextInt64(0, 1000)).ToArray();
        var quarterlyValues = Enumerable.Range(0, 96).Select(i => r.NextInt64(0, 250)).ToArray();

        var wantedResult = hourlyValues.ToList();
        wantedResult.AddRange(Enumerable.Range(0, 24).Select(i =>
            quarterlyValues[4 * i + 0] + quarterlyValues[4 * i + 1] + quarterlyValues[4 * i + 2] + quarterlyValues[4 * i + +3]));
        wantedResult = wantedResult.Select(x => x * 1000).ToList(); // kwh -> wh

        var hourlyQuantities = Enumerable.Range(0, 24).Select(i =>
            new EnergyQuantityValue()
            {
                Position = (i + 1).ToString(),
                EnergyQuantity = hourlyValues[i].ToString(),
                EnergyTimeSeriesMeasureUnit = "KWH",
                QuantityQuality = "E01",
                QuantityMissingIndicator = "false"
            });
        var mockedResponse = GenerateMeterTimeSeriesResponse(quantities: hourlyQuantities, dateOfReading: dateFrom.ToString("yyyy-MM-dd"),
            meterReadingOccurrence: occurenceOne);

        var quarterlyState = new MeteringPointState
        {
            MeterReadingOccurrence = occurenceTwo,
            SettlementMethod = "E02",
            ValidFromDate = "2020-01-01",
            ValidToDate = "2021-01-31"
        };
        var eQuantity = new NonProfiledEnergyQuantity
        {
            Date = dateFrom.AddDays(1).ToString("yyyy-MM-dd")
        };
        var quarterlyQuantities = Enumerable.Range(0, 96).Select(i =>
            new EnergyQuantityValue()
            {
                Position = (i + 1).ToString(),
                EnergyQuantity = quarterlyValues[i].ToString(),
                EnergyTimeSeriesMeasureUnit = "KWH",
                QuantityQuality = "E01",
                QuantityMissingIndicator = "false"
            });
        eQuantity.EnergyQuantityValues.AddRange(quarterlyQuantities);
        quarterlyState.NonProfiledEnergyQuantities.Add(eQuantity);

        mockedResponse.GetMeterTimeSeriesResult.MeterTimeSeriesMeteringPoint.First().MeteringPointStates.Add(quarterlyState);

        var clientMock = Substitute.For<Metertimeseries.V1.MeterTimeSeries.MeterTimeSeriesClient>();

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
            Gsrn = mockedResponse.GetMeterTimeSeriesResult.MeterTimeSeriesMeteringPoint.First().MeteringPointId,
            Actor = Guid.NewGuid().ToString(),
            Subject = Guid.NewGuid().ToString(),
            DateFrom = dateFrom.ToUnixTimeSeconds(),
            DateTo = dateTo.ToUnixTimeSeconds(),
        };

        var response = await client.GetMeasurementsAsync(request);

        response.Should().NotBeNull();
        response.Measurements.Count.Should().Be(48);

        response.Measurements.Select(m => m.Quantity).Should().BeEquivalentTo(wantedResult);
        Enumerable.Range(0, 48).Select(x => dateFrom.AddHours(x).ToUnixTimeSeconds()).Should()
            .BeEquivalentTo(response.Measurements.Select(x => x.DateFrom));
        Enumerable.Range(1, 48).Select(x => dateFrom.AddHours(x).ToUnixTimeSeconds()).Should()
            .BeEquivalentTo(response.Measurements.Select(x => x.DateTo));
    }

    [Fact]
    public async Task GetMeasurements_GivenEmptyTsResult_ReturnsEmptyList()
    {
        var response = await GetMeasurementsWithMockedResponse(new MeterTimeSeriesResponse
        {
            GetMeterTimeSeriesResult = new GetMeterTimeSeriesResult()
        });

        response.Should().NotBeNull();
        response.Measurements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMeasurements_GivenEmptyMPList_ReturnsEmptyList()
    {
        var response = await GetMeasurementsWithMockedResponse(new MeterTimeSeriesResponse
        {
            GetMeterTimeSeriesResult = new GetMeterTimeSeriesResult
            {
                MeterTimeSeriesMeteringPoint = { }
            }
        });

        response.Should().NotBeNull();
        response.Measurements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMeasurements_GivenEmptyMeteringPoint_ReturnsEmptyList()
    {
        var response = await GetMeasurementsWithMockedResponse(new MeterTimeSeriesResponse
        {
            GetMeterTimeSeriesResult = new GetMeterTimeSeriesResult
            {
                MeterTimeSeriesMeteringPoint = { new MeterTimeSeriesMeteringPoint() }
            }
        });

        response.Should().NotBeNull();
        response.Measurements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMeasurements_GivenEmptyStatesList_ReturnsEmptyList()
    {
        var response = await GetMeasurementsWithMockedResponse(new MeterTimeSeriesResponse
        {
            GetMeterTimeSeriesResult = new GetMeterTimeSeriesResult
            {
                MeterTimeSeriesMeteringPoint =
                {
                    new MeterTimeSeriesMeteringPoint
                    {
                        MeteringPointStates = { }
                    }
                }
            }
        });

        response.Should().NotBeNull();
        response.Measurements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMeasurements_GivenEmptyQuantitiesList_ReturnsEmptyList()
    {
        var response = await GetMeasurementsWithMockedResponse(new MeterTimeSeriesResponse
        {
            GetMeterTimeSeriesResult = new GetMeterTimeSeriesResult
            {
                MeterTimeSeriesMeteringPoint =
                {
                    new MeterTimeSeriesMeteringPoint
                    {
                        MeteringPointStates =
                        {
                            new MeteringPointState
                            {
                                NonProfiledEnergyQuantities = { }
                            }
                        }
                    }
                }
            }
        });

        response.Should().NotBeNull();
        response.Measurements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMeasurements_GivenEmptyValuesList_ReturnsEmptyList()
    {
        var response = await GetMeasurementsWithMockedResponse(new MeterTimeSeriesResponse
        {
            GetMeterTimeSeriesResult = new GetMeterTimeSeriesResult
            {
                MeterTimeSeriesMeteringPoint =
                {
                    new MeterTimeSeriesMeteringPoint
                    {
                        MeteringPointStates =
                        {
                            new MeteringPointState
                            {
                                NonProfiledEnergyQuantities =
                                {
                                    new NonProfiledEnergyQuantity
                                    {
                                        EnergyQuantityValues = { }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });

        response.Should().NotBeNull();
        response.Measurements.Should().BeEmpty();
    }

    private async Task<GetMeasurementsResponse> GetMeasurementsWithMockedResponse(MeterTimeSeriesResponse mockedResponse)
    {
        var dateFrom = DateTimeOffset.Parse("2022-01-01T00:00:00+01:00");
        var dateTo = DateTimeOffset.Parse("2022-01-01T02:00:00+01:00");

        var clientMock = Substitute.For<Metertimeseries.V1.MeterTimeSeries.MeterTimeSeriesClient>();

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
            DateFrom = dateFrom.ToUnixTimeSeconds(),
            DateTo = dateTo.ToUnixTimeSeconds(),
        };

        return await client.GetMeasurementsAsync(request);
    }

    private static MeterTimeSeriesResponse GenerateMeterTimeSeriesResponse(IEnumerable<EnergyQuantityValue>? quantities = null,
        string dateOfReading = "2021-01-31",
        int position = 1,
        string meterReadingOccurrence = "PT1H",
        string gsrn = "1234567890123456",
        double quantity = 0.88,
        string quantityMissingIndicator = "false")
    {
        var mockedResponse = new MeterTimeSeriesResponse();
        mockedResponse.GetMeterTimeSeriesRejection = null;
        var mp = new MeterTimeSeriesMeteringPoint
        {
            TypeOfMp = "E17",
            ConsumerStartDate = "2017-12-31T23:00:00Z",
            MeteringPointId = gsrn //Gsrn
        };
        var mpState = new MeteringPointState
        {
            MeterReadingOccurrence = meterReadingOccurrence, //See position
            SettlementMethod = "E02",
            ValidFromDate = "2020-01-01",
            ValidToDate = "2021-01-31"
        };
        var eQuantity = new NonProfiledEnergyQuantity
        {
            Date = dateOfReading //The day of the reading
        };

        if (quantities == null)
        {
            var qValue = new EnergyQuantityValue
            {
                EnergyQuantity = quantity.ToString(CultureInfo.InvariantCulture),
                EnergyTimeSeriesMeasureUnit = "KWH",
                Position = position.ToString(), //Meaning timeposition. Meaning 01 hour with PT1H. Meaning 15 minutes with PT15M
                QuantityQuality = "E01", //Measured
                QuantityMissingIndicator = quantityMissingIndicator
            };
            eQuantity.EnergyQuantityValues.Add(qValue);
        }
        else
        {
            eQuantity.EnergyQuantityValues.AddRange(quantities);
        }

        mpState.NonProfiledEnergyQuantities.Add(eQuantity);
        mp.MeteringPointStates.Add(mpState);
        mockedResponse.GetMeterTimeSeriesResult = new GetMeterTimeSeriesResult
        {
            MeterTimeSeriesMeteringPoint = { mp }
        };
        return mockedResponse;
    }
}
