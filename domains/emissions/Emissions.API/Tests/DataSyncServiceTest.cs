using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using API.Services;
using EnergyOriginAuthorization;
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
        var mockClient = MockHttpClientFactory.SetupHttpClientWithFiles(new List<string> { "datasync_meteringpoints.json" });
        var datasync = new DataSyncService(mockClient);

        var res = await datasync.GetListOfMeteringPoints(new AuthorizationContext("", "", ""));

        Assert.NotEmpty(res);
        Assert.Equal(3, res.Count());
        Assert.Equal("571313121223234323", res.First().GSRN);
        Assert.Equal("DK1", res.First().GridArea);
        Assert.Equal(MeterType.Consumption, res.First().Type);
    }

    [Fact]
    public async void DataSync_GetMeasurements_success()
    {
        var mockClient = MockHttpClientFactory.SetupHttpClientWithFiles(new List<string> { "datasync_measurements.json" });
        var dateFrom = 1609455600L;
        var dateTo = 1609459200L;
        var datasync = new DataSyncService(mockClient);

        var res = await datasync.GetMeasurements(new AuthorizationContext("", "", ""), "571313121223234323", new DateTimeOffset(), new DateTimeOffset());

        Assert.NotEmpty(res);
        Assert.Equal(2, res.Count());
        Assert.Equal("571313121223234323", res.First().GSRN);
        Assert.Equal(dateFrom, res.First().DateFrom);
        Assert.Equal(dateTo, res.First().DateTo);
        Assert.Equal(1250L, res.First().Quantity);
        Assert.Equal(Quality.Measured, res.First().Quality);
    }
}
