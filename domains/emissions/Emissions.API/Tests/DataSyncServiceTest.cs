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
public sealed class DataSyncServiceTest
{
    [Fact]
    public async void DataSync_GetListOfMeteringPoints_success()
    {
        // Arrange
        var mockClient = SetupHttpClientFromFile("datasync_meteringpoints.json");

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
        Assert.Equal(MeterType.consumption, res.First().Type);
    }

    [Fact]
    public async void DataSync_GetMeasurements_success()
    {
        // Arrange
        var mockClient = SetupHttpClientFromFile("datasync_measurements.json");

        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var logger = new Mock<ILogger<DataSyncService>>();

        var datasync = new DataSyncService(logger.Object, mockClient);

        // Act
        var res = await datasync.GetMeasurements(new AuthorizationContext("", "", ""), "571313121223234323", new DateTime(), new DateTime());

        // Assert
        Assert.NotEmpty(res);
        Assert.Equal(2, res.Count());

        Assert.Equal("571313121223234323", res.First().GSRN);
        Assert.Equal(1609455600, res.First().DateFrom);
        Assert.Equal(1609459200, res.First().DateTo);
        Assert.Equal(1250, res.First().Quantity);
        Assert.Equal(Quality.Measured, res.First().Quality);
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
