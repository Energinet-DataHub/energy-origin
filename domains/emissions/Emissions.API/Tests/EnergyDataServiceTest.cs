using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Services;
using AutoFixture;
using EnergyOriginAuthorization;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class EnergyDataServiceTest
{

    [Fact]
    public async void DatePeriod_GetEmissionsPerHour_EmissionRecordsReturned()
    {
        // Arrange
        var edsMock = SetupHttpClientFromFile("eds_emissions_hourly.json");

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
        var edsMock = SetupHttpClientFromFile("eds_mix_hourly.json");

        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var logger = new Mock<ILogger<EnergiDataService>>();

        var eds = new EnergiDataService(logger.Object, edsMock);

        // Act
        var res = await eds.GetResidualMixPerHour(dateFrom, dateTo);

        // Assert
        Assert.NotEmpty(res);
        Assert.Equal(5, res.Count());
    }


    HttpClient SetupHttpClientFromFile(string resourceName)
    {
        var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Invalid directory");
        var path = System.IO.Path.Combine(directory, "../../../Resources/", resourceName);
        string json = File.ReadAllText(path);
        return SetupHttpClient(json);
    }

    HttpClient SetupHttpClient(string serialize)
    {

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            // Setup the PROTECTED method to mock
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            // prepare the expected response of the mocked http call
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(serialize),
            }).Verifiable();


        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test.com/"),
        };
    }
}
