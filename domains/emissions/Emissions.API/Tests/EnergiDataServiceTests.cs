using System;
using System.Collections.Generic;
using System.Linq;
using API.Services;
using Tests.Helpers;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class EnergiDataServiceTests
{
    [Fact]
    public async void DatePeriod_GetEmissionsPerHour_EmissionRecordsReturned()
    {
        var edsMock = MockHttpClientFactory.SetupHttpClientWithFiles(new List<string> { "eds_emissions_hourly.json" });
        var dateFrom = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var dateTo = new DateTimeOffset(2021, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var eds = new EnergiDataService(edsMock);

        var res = await eds.GetEmissionsPerHour(dateFrom, dateTo);

        Assert.NotEmpty(res);
        Assert.Equal(10, res.Count());
    }

    [Fact]
    public async void DatePeriod_GetResidualMixPerHour_EmissionRecordsReturned()
    {
        var edsMock = MockHttpClientFactory.SetupHttpClientWithFiles(new List<string> { "eds_mix_hourly_all.json", "eds_mix_hourly_total.json" });
        var dateFrom = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var dateTo = new DateTimeOffset(2021, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var eds = new EnergiDataService(edsMock);

        var res = await eds.GetResidualMixPerHour(dateFrom, dateTo);

        Assert.NotEmpty(res);
        Assert.Equal(24, res.Count());
    }
}
