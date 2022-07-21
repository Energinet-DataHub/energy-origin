using System;
using System.Linq;
using API.Services;
using Microsoft.Extensions.Logging;
using Moq;
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
        // Arrange
        var edsMock = MockHttpClientFactory.SetupHttpClientFromFile("eds_emissions_hourly.json");

        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var logger = new Mock<ILogger<EnergiDataService>>();

        var eds = new EnergiDataService(logger.Object, edsMock);

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
        var edsMock = MockHttpClientFactory.SetupHttpClientFromFile("eds_mix_hourly.json");

        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var logger = new Mock<ILogger<EnergiDataService>>();

        var eds = new EnergiDataService(logger.Object, edsMock);

        // Act
        var res = await eds.GetResidualMixPerHour(dateFrom, dateTo);

        // Assert
        Assert.NotEmpty(res);
        Assert.Equal(10, res.Count());
    }
}
