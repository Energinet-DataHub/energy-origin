using System;
using System.Collections.Generic;
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
public sealed class EmissionsServiceTest
{
    readonly CalculateEmissionDataSetFactory dataSetFactory = new();

    [Fact]
    public async void DatePeriod_GetEmissions_EmissionRecordsReturned()
    {
        // Arrange
        var result = new Fixture().Create<EmissionsDataResponse>();
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var serialize = JsonSerializer.Serialize(result, options);
        var edsMock = SetupHttpClient(serialize);

        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var logger = new Mock<ILogger<EmissionDataService>>();

        var sut = new EmissionDataService(logger.Object, edsMock);

        // Act
        var res = await sut.GetEmissionsPerHour(dateFrom, dateTo);

        // Assert
        Assert.NotNull(res);
        Assert.NotEmpty(res.Result.EmissionRecords);
    }

    [Fact]
    public async void ListOfMeteringPoints_GetTimeSeries_Measurements()
    {
        //Arrange
        var context = new AuthorizationContext("subject", "actor", "token");
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var meteringPoints = new Fixture().Create<List<MeteringPoint>>();
        var measurements = dataSetFactory.CreateMeasurements();

        var mockDataSyncService = new Mock<IDataSyncService>();
        var mockEds = new Mock<IEmissionDataService>();
        var mockEmissionsCalculator = new Mock<IEmissionsCalculator>();
        var mockSourcesCalculator = new Mock<ISourcesCalculator>();

        mockDataSyncService.Setup(a => a.GetMeasurements(It.IsAny<AuthorizationContext>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Aggregation>()))
            .Returns(Task.FromResult(measurements.AsEnumerable()));

        var sut = new EmissionsService(mockDataSyncService.Object, mockEds.Object, mockEmissionsCalculator.Object, mockSourcesCalculator.Object);
        //Act

        var timeSeries = (await sut.GetTimeSeries(context,
            ((DateTimeOffset)DateTime.SpecifyKind(dateFrom, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            ((DateTimeOffset)DateTime.SpecifyKind(dateTo, DateTimeKind.Utc)).ToUnixTimeSeconds(), Aggregation.Hour,
            meteringPoints)).ToArray();
        //Assert

        Assert.NotNull(timeSeries);
        Assert.NotEmpty(timeSeries);
        Assert.Equal(measurements.Count, timeSeries.First().Measurements.Count());
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
