using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Models;
using API.Services;
using AutoFixture;
using EnergyOriginAuthorization;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class AggregationServiceTest
{
    readonly ConsumptionAggregationDataSetFactory dataSetFactory = new();

    [Fact]
    public async void MeasurementsService_GetTimeSeries_Measurements()
    {
        //Arrange
        var context = new AuthorizationContext("subject", "actor", "token");
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var meteringPoints = new Fixture().Create<List<MeteringPoint>>();
        var measurements = dataSetFactory.CreateMeasurements();

        var mockDataSyncService = new Mock<IDataSyncService>();

        mockDataSyncService.Setup(a => a.GetMeasurements(
            It.IsAny<AuthorizationContext>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()))
            .Returns(Task.FromResult(measurements.AsEnumerable()));

        var sut = new MeasurementsService(mockDataSyncService.Object, new Mock<IConsumptionAggregation>().Object);

        //Act

        var timeSeries = (await sut.GetTimeSeries(context,
            ((DateTimeOffset)DateTime.SpecifyKind(dateFrom, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            ((DateTimeOffset)DateTime.SpecifyKind(dateTo, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            Aggregation.Hour,
            meteringPoints)).ToArray();

        //Assert

        Assert.NotNull(timeSeries);
        Assert.NotEmpty(timeSeries);
        Assert.Equal(measurements.Count, timeSeries.First().Measurements.Count());
    }
}
