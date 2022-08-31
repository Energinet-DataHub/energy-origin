using System;
using System.Collections.Generic;
using System.Linq;
using API.Shared.DataSync;
using API.Shared.DataSync.Models;
using EnergyOriginAuthorization;
using Tests.Helpers;
using Xunit;

namespace Tests;

public sealed class DataSyncServiceTest
{
    [Fact]
    public async void DataSync_GetListOfMeteringPoints_success()
    {
        // Arrange
        var datasyncData = new List<string>(new string[] { "datasync_meteringpoints.json" });
        var mockClient = MockHttpClientFactory.SetupHttpClientWithFiles(datasyncData);

        var datasync = new DataSyncService(mockClient);

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
        var datasyncData = new List<string>(new string[] { "datasync_measurements.json" });
        var mockClient = MockHttpClientFactory.SetupHttpClientWithFiles(datasyncData);

        var dateFrom = 1609455600L;
        var dateTo = 1609459200L;

        var datasync = new DataSyncService(mockClient);

        // Act
        var res = await datasync.GetMeasurements(new AuthorizationContext("", "", ""), "571313121223234323", new DateTime(), new DateTime());

        // Assert
        Assert.NotEmpty(res);
        Assert.Equal(2, res.Count());

        Assert.Equal("571313121223234323", res.First().GSRN);
        Assert.Equal(dateFrom, res.First().DateFrom);
        Assert.Equal(dateTo, res.First().DateTo);
        Assert.Equal(1250L, res.First().Quantity);
        Assert.Equal(Quality.Measured, res.First().Quality);
    }


}
