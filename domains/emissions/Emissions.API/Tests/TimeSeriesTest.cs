using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using API.Shared.DataSync;
using API.Shared.DataSync.Models;
using AutoFixture;
using EnergyOriginAuthorization;
using Moq;
using Tests;
using Xunit;

namespace API.Tests;

public sealed class TimeSeriesTests
{

    [Fact]
    public async void ListOfMeteringPoints_GetTimeSeries_Measurements()
    {
        //Arrange
        var context = new AuthorizationContext("subject", "actor", "token");
        var dateFrom = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var dateTo = new DateTimeOffset(2021, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var meteringPoints = new Fixture().Create<List<MeteringPoint>>();
        var measurements = CalculateEmissionDataSetFactory.CreateMeasurementsFirstMP();

        var mockDataSyncService = new Mock<IDataSyncService>();

        mockDataSyncService.Setup(it =>
                it.GetMeasurements(
                    It.IsAny<AuthorizationContext>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>()
                )
            )
            .Returns(Task.FromResult(measurements.AsEnumerable()));

        //Act
        var timeSeries = await mockDataSyncService.Object.GetTimeSeries(context, dateFrom, dateTo, meteringPoints);

        //Assert

        Assert.NotNull(timeSeries);
        Assert.NotEmpty(timeSeries);
        Assert.Equal(measurements.Count, timeSeries.First().Measurements.Count());
    }
}
