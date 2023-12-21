using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using API.Models;
using API.Services;
using AutoFixture;
using NSubstitute;
using Tests.Helpers;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class MeasurementsServiceTest
{
    [Fact]
    public async void ListOfMeteringPoints_GetTimeSeries_Measurements()
    {
        var token = "dummyBearerToken";
        var authHeader = new AuthenticationHeaderValue("Bearer", token);
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var meteringPoints = new Fixture().Create<List<MeteringPoint>>();
        var measurements = MeasurementDataSet.CreateMeasurements();

        var mockDataSyncService = Substitute.For<IDataSyncService>();

        mockDataSyncService.GetMeasurements(
            Arg.Any<AuthenticationHeaderValue>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(Task.FromResult(measurements.AsEnumerable()));

        var sut = new MeasurementsService(mockDataSyncService, Substitute.For<IAggregator>());

        var timeSeries = await sut.GetTimeSeries(
            authHeader,
            dateFrom,
            dateTo,
            meteringPoints
        );

        timeSeries = timeSeries.ToArray();

        Assert.NotNull(timeSeries);
        Assert.NotEmpty(timeSeries);
        Assert.Equal(measurements.Count, timeSeries.First().Measurements.Count());
    }
}
