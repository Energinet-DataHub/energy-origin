using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Services;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Domain.ValueObjects;
using Meteringpoint.V1;
using NSubstitute;
using Xunit;
using EnergyTrackAndTrace.Testing.Extensions;
using EnergyOrigin.Datahub3;
using Energinet.DataHub.Measurements.Abstractions.Api.Models;
using NodaTime;

namespace API.UnitTests.Services;

public class ConsumptionServiceTests
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _meteringPointClientMock;
    private readonly IDataHubFacadeClient _dhFacadeClientMock;
    private readonly IMeasurementClient _measurementClientMock;

    public ConsumptionServiceTests()
    {
        _meteringPointClientMock = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        _dhFacadeClientMock = Substitute.For<IDataHubFacadeClient>();
        _measurementClientMock = Substitute.For<IMeasurementClient>();
    }

    [Fact]
    public async Task GetTotalHourlyConsumption_Expect24Entries()
    {
        var subject = Guid.NewGuid();
        var dateTo = DateTimeOffset.Now;
        var dateFrom = dateTo.AddDays(-30);
        var gsrn = Any.Gsrn();

        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn)
                }
            });

        _dhFacadeClientMock.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(new ListMeteringPointForCustomerCaResponse
        {
            Relations =
            [
                new () { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }
            ],
            Rejections = new List<Rejection>()
        });

        var mpData = EnergyTrackAndTrace.Testing.Any.MeasurementsApiResponse(gsrn, dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), 100);

        _measurementClientMock.GetMeasurements(Arg.Any<List<Gsrn>>(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(mpData);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _measurementClientMock);

        var (total, average) = await sut.GetTotalAndAverageHourlyConsumption(OrganizationId.Create(subject), dateFrom, dateTo, new CancellationToken());

        Assert.Equal(24, total.Count);
        Assert.Equal(24, average.Count);
    }

    [Fact]
    public async Task GetTotalHourlyConsumption_WhenProductionMeteringPointAndConsumptionMeteringPoint_ExpectGetMeasurementsOnlyCalledWithConsumptionMeteringPoint()
    {
        var subject = Guid.NewGuid();
        var dateTo = DateTimeOffset.Now;
        var dateFrom = dateTo.AddDays(-30);
        var consumptionGsrn = Any.Gsrn();
        var productionGsrn = Any.Gsrn();

        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(consumptionGsrn),
                    EnergyTrackAndTrace.Testing.Any.ProductionMeteringPoint(productionGsrn)
                }
            });

        _dhFacadeClientMock.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(new ListMeteringPointForCustomerCaResponse
        {
            Relations =
            [
                new () { MeteringPointId = consumptionGsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) },
                new () { MeteringPointId = productionGsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }
            ],
            Rejections = new List<Rejection>()
        });

        var mpDataConsumption = EnergyTrackAndTrace.Testing.Any.MeasurementsApiResponse(consumptionGsrn, dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), 100);
        var mpDataProduction = EnergyTrackAndTrace.Testing.Any.MeasurementsApiResponse(productionGsrn, dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), 100);
        var both = mpDataConsumption.Concat(mpDataProduction).ToArray();

        _measurementClientMock.GetMeasurements(Arg.Any<List<Gsrn>>(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(mpDataConsumption);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _measurementClientMock);

        var (total, average) = await sut.GetTotalAndAverageHourlyConsumption(OrganizationId.Create(subject), dateFrom, dateTo, new CancellationToken());

        await _measurementClientMock.Received(1)
            .GetMeasurements(Arg.Is((IList<Gsrn> x) => x.First() == consumptionGsrn && x.Count == 1), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTotalHourlyConsumption_WhenThirtyDaysOfData_Expect30DaysOfDataDistributedOn24Hour()
    {
        var subject = Guid.NewGuid();
        var numberOfDays = 30;
        var dateTo = new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero);
        var dateFrom = dateTo.AddDays(-numberOfDays);
        var gsrn = Any.Gsrn();

        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn)
                }
            });

        _dhFacadeClientMock.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(new ListMeteringPointForCustomerCaResponse
        {
            Relations =
            [
                new () { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }
            ],
            Rejections = new List<Rejection>()
        });

        var mpData = EnergyTrackAndTrace.Testing.Any.MeasurementsApiResponse(gsrn, dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), 100, 1);

        _measurementClientMock.GetMeasurements(Arg.Any<IList<Gsrn>>(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(mpData);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _measurementClientMock);

        var (total, average) = await sut.GetTotalAndAverageHourlyConsumption(OrganizationId.Create(subject), dateFrom, dateTo, new CancellationToken());

        foreach (var hour in total)
        {
            Assert.Equal(numberOfDays, hour.KwhQuantity);
        }
        foreach (var hour in average)
        {
            Assert.Equal(1, hour.KwhQuantity);
        }
    }

    [Fact]
    public async Task ComputeHourlyAverages_WhenMultipleMeteringPointsGenerateDataForDiffentPeriods_ExpectAverage()
    {
        var subject = Guid.NewGuid();
        var numberOfDays = 30;
        var halfOfPeriod = numberOfDays / 2;
        var fullPeriodDateTo = new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero);
        var fullPeriodDateFrom = fullPeriodDateTo.AddDays(-numberOfDays);
        var gsrn = Any.Gsrn();
        var gsrn2 = Any.Gsrn();

        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn),
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn2)
                }
            });

        _dhFacadeClientMock.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(new ListMeteringPointForCustomerCaResponse
        {
            Relations =
            [
                new () { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) },
                new () { MeteringPointId = gsrn2.Value, ValidFromDate = DateTime.Now.AddHours(-1) }
            ],
            Rejections = []
        });

        var gsrnQuantity = 1;
        var gsrn2Quantity = 3;
        var mpData = EnergyTrackAndTrace.Testing.Any.MeasurementsApiResponse(gsrn, fullPeriodDateFrom.ToUnixTimeSeconds(), fullPeriodDateTo.AddDays(-halfOfPeriod).ToUnixTimeSeconds(), 100, gsrnQuantity).ToList();
        var mpData2 = EnergyTrackAndTrace.Testing.Any.MeasurementsApiResponse(gsrn2, fullPeriodDateFrom.AddDays(halfOfPeriod).ToUnixTimeSeconds(), fullPeriodDateTo.ToUnixTimeSeconds(), 100, gsrn2Quantity).ToList();
        mpData.AddRange(mpData2);

        _measurementClientMock.GetMeasurements(Arg.Any<IList<Gsrn>>(), fullPeriodDateFrom.ToUnixTimeSeconds(), fullPeriodDateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(mpData);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _measurementClientMock);

        var (_, average) = await sut.GetTotalAndAverageHourlyConsumption(OrganizationId.Create(subject), fullPeriodDateFrom, fullPeriodDateTo, new CancellationToken());

        foreach (var hour in average)
        {
            Assert.Equal(2, hour.KwhQuantity);
        }
    }

    [Fact]
    public async Task ComputeHourlyAverages_WhenMultipleMeteringPointsGenerateDataForTheSamePeriod_ExpectAverage()
    {
        var subject = Guid.NewGuid();
        var numberOfDays = 30;
        var fullPeriodDateTo = new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero);
        var fullPeriodDateFrom = fullPeriodDateTo.AddDays(-numberOfDays);
        var gsrn = Any.Gsrn();
        var gsrn2 = Any.Gsrn();

        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn),
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn2)
                }
            });

        _dhFacadeClientMock.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(new ListMeteringPointForCustomerCaResponse
        {
            Relations =
            [
                new () { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) },
                new () { MeteringPointId = gsrn2.Value, ValidFromDate = DateTime.Now.AddHours(-1) }
            ],
            Rejections = []
        });

        var gsrnQuantity = 1;
        var gsrn2Quantity = 3;
        var mpData = EnergyTrackAndTrace.Testing.Any.MeasurementsApiResponse(gsrn, fullPeriodDateFrom.ToUnixTimeSeconds(), fullPeriodDateTo.ToUnixTimeSeconds(), 100, gsrnQuantity).ToList();
        var mpData2 = EnergyTrackAndTrace.Testing.Any.MeasurementsApiResponse(gsrn2, fullPeriodDateFrom.ToUnixTimeSeconds(), fullPeriodDateTo.ToUnixTimeSeconds(), 100, gsrn2Quantity).ToList();
        mpData.AddRange(mpData2);

        _measurementClientMock.GetMeasurements(Arg.Any<IList<Gsrn>>(), fullPeriodDateFrom.ToUnixTimeSeconds(), fullPeriodDateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(mpData);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _measurementClientMock);

        var (_, average) = await sut.GetTotalAndAverageHourlyConsumption(OrganizationId.Create(subject), fullPeriodDateFrom, fullPeriodDateTo, new CancellationToken());

        foreach (var hour in average)
        {
            Assert.Equal(4, hour.KwhQuantity);
        }
    }

    [Fact]
    public async Task GetTotalHourlyConsumption_WhenExcludingHour2_ExpectHour2IsZero()
    {
        var subject = Guid.NewGuid();
        var dateTo = DateTimeOffset.Now;
        var dateFrom = dateTo.AddDays(-30);
        var gsrn = Any.Gsrn();

        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn)
                }
            });

        _dhFacadeClientMock.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(new ListMeteringPointForCustomerCaResponse
        {
            Relations =
            [
                new () { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }
            ],
            Rejections = new List<Rejection>()
        });

        var mpData = EnergyTrackAndTrace.Testing.Any.MeasurementsApiResponse(gsrn, dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), 100);

        foreach (var mp in mpData)
        {
            var groups = new List<PointAggregationGroup>();
            foreach (var day in mp.PointAggregationGroups)
            {
                var aggs = day.Value.PointAggregations.RemoveAll(x => DateTimeOffset.FromUnixTimeSeconds(x.From.ToUnixTimeSeconds()).Hour == 2);
            }
        }

        _measurementClientMock.GetMeasurements(Arg.Any<List<Gsrn>>(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(mpData);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _measurementClientMock);

        var (total, average) = await sut.GetTotalAndAverageHourlyConsumption(OrganizationId.Create(subject), dateFrom, dateTo, new CancellationToken());

        Assert.Equal(0, total.First(x => x.HourOfDay == 2).KwhQuantity);
        Assert.Equal(0, average.First(x => x.HourOfDay == 2).KwhQuantity);
    }

    [Fact]
    public async Task ComputeHourlyAverages_Test_ExpectAverage()
    {
        var subject = Guid.NewGuid();
        var dateTo = new DateTimeOffset(2025, 7, 18, 0, 0, 0, TimeSpan.Zero);
        var dateFrom = new DateTimeOffset(2025, 7, 16, 0, 0, 0, TimeSpan.Zero);
        var gsrn = Any.Gsrn();
        var gsrn2 = Any.Gsrn();

        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn),
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn2)
                }
            });

        _dhFacadeClientMock.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(new ListMeteringPointForCustomerCaResponse
        {
            Relations =
            [
                new () { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) },
                new () { MeteringPointId = gsrn2.Value, ValidFromDate = DateTime.Now.AddHours(-1) }
            ],
            Rejections = []
        });

        var pags1 = new Dictionary<string, PointAggregationGroup>();

        var pag1 = new PointAggregationGroup(
                Instant.FromUnixTimeSeconds(dateFrom.ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateTo.ToUnixTimeSeconds()),
                Resolution.Hourly,
                []);

        pag1.PointAggregations.Add(new PointAggregation(
                Instant.FromUnixTimeSeconds(dateFrom.ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateFrom.AddHours(1).ToUnixTimeSeconds()),
                1800,
                Quality.Measured));

        pag1.PointAggregations.Add(new PointAggregation(
                Instant.FromUnixTimeSeconds(dateFrom.AddHours(1).ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateFrom.AddHours(2).ToUnixTimeSeconds()),
                1900,
                Quality.Measured));

        pag1.PointAggregations.Add(new PointAggregation(
                Instant.FromUnixTimeSeconds(dateFrom.AddDays(1).ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateFrom.AddDays(1).AddHours(1).ToUnixTimeSeconds()),
                3000,
                Quality.Measured));

        pag1.PointAggregations.Add(new PointAggregation(
                Instant.FromUnixTimeSeconds(dateFrom.AddDays(1).AddHours(1).ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateFrom.AddDays(1).AddHours(2).ToUnixTimeSeconds()),
                4000,
                Quality.Measured));

        var pags2 = new Dictionary<string, PointAggregationGroup>();
        var pag2 = new PointAggregationGroup(
                Instant.FromUnixTimeSeconds(dateFrom.ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateTo.ToUnixTimeSeconds()),
                Resolution.Hourly,
                []);

        pag2.PointAggregations.Add(new PointAggregation(
                Instant.FromUnixTimeSeconds(dateFrom.ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateFrom.AddHours(1).ToUnixTimeSeconds()),
                200,
                Quality.Measured));

        pag2.PointAggregations.Add(new PointAggregation(
                Instant.FromUnixTimeSeconds(dateFrom.AddHours(1).ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateFrom.AddHours(2).ToUnixTimeSeconds()),
                300,
                Quality.Measured));

        pag2.PointAggregations.Add(new PointAggregation(
                Instant.FromUnixTimeSeconds(dateFrom.AddDays(1).ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateFrom.AddDays(1).AddHours(1).ToUnixTimeSeconds()),
                1000,
                Quality.Measured));

        pag2.PointAggregations.Add(new PointAggregation(
                Instant.FromUnixTimeSeconds(dateFrom.AddDays(1).AddHours(1).ToUnixTimeSeconds()),
                Instant.FromUnixTimeSeconds(dateFrom.AddDays(1).AddHours(2).ToUnixTimeSeconds()),
                2000,
                Quality.Measured));

        pags1.Add("DoesNotMatter1", pag1);
        pags2.Add("DoesNotMatter2", pag2);

        _measurementClientMock.GetMeasurements(Arg.Any<IList<Gsrn>>(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(
                    [
                    new MeasurementAggregationByPeriodDto(new Energinet.DataHub.Measurements.Abstractions.Api.Models.MeteringPoint(gsrn.Value), pags1),
                    new MeasurementAggregationByPeriodDto(new Energinet.DataHub.Measurements.Abstractions.Api.Models.MeteringPoint(gsrn2.Value), pags2)
                    ]);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _measurementClientMock);

        var (_, average) = await sut.GetTotalAndAverageHourlyConsumption(OrganizationId.Create(subject), dateFrom, dateTo, new CancellationToken());

        Assert.Equal(3000, average[0].KwhQuantity);
        Assert.Equal(4100, average[1].KwhQuantity);
        Assert.Equal(0, average[2].KwhQuantity);
        Assert.Equal(0, average[23].KwhQuantity);
    }
}
