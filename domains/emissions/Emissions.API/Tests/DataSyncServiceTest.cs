using System;
using System.Linq;
using API.Models;
using API.Services;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Helpers;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class DataSyncServiceTest
{
    [Fact]
    public async void DataSync_GetListOfMeteringPoints_success()
    {
        // Arrange
        var mockClient = MockHttpClientFactory.SetupHttpClientFromFile("datasync_meteringpoints.json");

        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var logger = new Mock<ILogger<DataSyncService>>();

        var datasync = new DataSyncService(logger.Object, mockClient);

        // Act
        var res = await datasync.GetListOfMeteringPoints(new AuthorizationContext("", "", ""));

        // Assert
        Assert.NotEmpty(res);
        Assert.Equal(3, res.Count());

        Assert.Equal("571313121223234323", res.First().GSRN);
        Assert.Equal("DK1", res.First().GridArea);
        Assert.Equal(MeterType.Consumption, res.First().Type);
    }

    [Fact]
    public async void DataSync_GetMeasurements_success()
    {
        // Arrange
        var mockClient = MockHttpClientFactory.SetupHttpClientFromFile("datasync_measurements.json");

        var dateFrom = new DateTime(2020, 12, 31, 23, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var logger = new Mock<ILogger<DataSyncService>>();

        var datasync = new DataSyncService(logger.Object, mockClient);

        // Act
        var res = await datasync.GetMeasurements(new AuthorizationContext("", "", ""), "571313121223234323", new DateTime(), new DateTime());

        // Assert
        Assert.NotEmpty(res);
        Assert.Equal(2, res.Count());

        Assert.Equal("571313121223234323", res.First().GSRN);
        Assert.Equal(dateFrom, res.First().DateFrom);
        Assert.Equal(dateTo, res.First().DateTo);
        Assert.Equal(1250, res.First().Quantity);
        Assert.Equal(Quality.Measured, res.First().Quality);
    }
}
