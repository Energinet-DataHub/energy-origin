using System;
using System.Collections.Generic;
using System.Linq;
using API.Services;
using Tests.Helpers;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class EnergiDataServiceTest
{
    [Fact]
    public async void DatePeriod_GetEmissionsPerHour_EmissionRecordsReturned()
    {
        var edsMock = MockHttpClientFactory.SetupHttpClientWithFiles(new List<string>(new string[] { "eds_emissions_hourly.json" }));
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var eds = new EnergiDataService(edsMock);

        var res = await eds.GetEmissionsPerHour(dateFrom, dateTo);

        Assert.NotEmpty(res);
        Assert.Equal(10, res.Count());
    }

    [Fact]
    public async void DatePeriod_GetResidualMixPerHour_EmissionRecordsReturned()
    {
        var edsMock = MockHttpClientFactory.SetupHttpClientWithFiles(new List<string>(new string[] { "eds_mix_hourly_all.json", "eds_mix_hourly_total.json" }));
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var eds = new EnergiDataService(edsMock);

        var res = await eds.GetResidualMixPerHour(dateFrom, dateTo);

        Assert.NotEmpty(res);
        Assert.Equal(24, res.Count());
    }
}
