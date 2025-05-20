using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Services;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Domain.ValueObjects;
using Meteringpoint.V1;
using NSubstitute;
using Xunit;
using EnergyTrackAndTrace.Testing.Extensions;

namespace API.UnitTests.Services;

public class ConsumptionServiceTests
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _meteringPointClientMock;
    private readonly IDataHubFacadeClient _dhFacadeClientMock;
    private readonly IDataHub3Client _dh3ClientMock;

    public ConsumptionServiceTests()
    {
        _meteringPointClientMock = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        _dhFacadeClientMock = Substitute.For<IDataHubFacadeClient>();
        _dh3ClientMock = Substitute.For<IDataHub3Client>();
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

        var mpData = EnergyTrackAndTrace.Testing.Any.TimeSeriesApiResponse(gsrn, dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), 100);

        _dh3ClientMock.GetMeasurements(Arg.Any<List<Gsrn>>(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(mpData);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _dh3ClientMock);

        var result = await sut.GetTotalHourlyConsumption(OrganizationId.Create(subject), dateFrom, dateTo, new CancellationToken());

        Assert.Equal(24, result.Count);
    }

    [Fact]
    public async Task GetTotalHourlyConsumption()
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

        var mpData = EnergyTrackAndTrace.Testing.Any.TimeSeriesApiResponse(gsrn, dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), 100);

        foreach (var mp in mpData)
        {
            foreach (var day in mp.PointAggregationGroups)
            {
                for (int i = 0; i < 24; i++)
                {
                    day.Value.PointAggregations.First(x => DateTimeOffset.FromUnixTimeSeconds(x.MinObservationTime).Hour == i).AggregatedQuantity = 1;
                }
            }
        }

        _dh3ClientMock.GetMeasurements(Arg.Any<List<Gsrn>>(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(mpData);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _dh3ClientMock);

        var result = await sut.GetTotalHourlyConsumption(OrganizationId.Create(subject), dateFrom, dateTo, new CancellationToken());

        foreach (var hour in result)
        {
            Assert.Equal(numberOfDays, hour.KwhQuantity);
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

        var mpData = EnergyTrackAndTrace.Testing.Any.TimeSeriesApiResponse(gsrn, dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), 100);

        foreach (var mp in mpData)
        {
            foreach (var day in mp.PointAggregationGroups)
            {
                day.Value.PointAggregations = day.Value.PointAggregations.Where(x => DateTimeOffset.FromUnixTimeSeconds(x.MinObservationTime).Hour != 2).ToList();
            }
        }

        _dh3ClientMock.GetMeasurements(Arg.Any<List<Gsrn>>(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), Arg.Any<CancellationToken>())
            .Returns(mpData);

        var sut = new ConsumptionService(_meteringPointClientMock, _dhFacadeClientMock, _dh3ClientMock);

        var result = await sut.GetTotalHourlyConsumption(OrganizationId.Create(subject), dateFrom, dateTo, new CancellationToken());

        Assert.Equal(0, result.First(x => x.HourOfDay == 2).KwhQuantity);
    }
}
