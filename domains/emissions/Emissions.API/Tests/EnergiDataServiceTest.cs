using System;
using System.Collections.Generic;
using System.Linq;
using API.Shared.EnergiDataService;
using Tests.Helpers;
using Xunit;

namespace Tests;

public sealed class EnergiDataServiceTest
{

    [Fact]
    public async void DatePeriod_GetEmissionsPerHour_EmissionRecordsReturned()
    {
        // Arrange
        var edsMmix = new List<string>(new string[] { "eds_emissions_hourly.json" });
        var edsMock = MockHttpClientFactory.SetupHttpClientWithFiles(edsMmix);

        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);

        var eds = new EnergiDataService(edsMock);

        // Act
        var res = await eds.GetEmissionsPerHour(dateFrom, dateTo);

        // Assert
        Assert.NotEmpty(res);
        Assert.Equal(10, res.Count());
    }

    [Fact]
    public async void DatePeriod_GetResidualMixPerHour_EmissionRecordsReturned()
    {
        // Arrange
        var edsData = new List<string>(new string[] { "eds_mix_hourly_all.json", "eds_mix_hourly_total.json" });
        var edsMock = MockHttpClientFactory.SetupHttpClientWithFiles(edsData);

        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);

        var eds = new EnergiDataService(edsMock);

        // Act
        var res = await eds.GetResidualMixPerHour(dateFrom, dateTo);

        // Assert
        Assert.NotEmpty(res);
        Assert.Equal(24, res.Count());
    }
}
