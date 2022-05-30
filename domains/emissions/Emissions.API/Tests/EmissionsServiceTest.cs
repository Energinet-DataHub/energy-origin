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
using Moq;
using Moq.Protected;
using Xunit;

namespace Tests;

public class EmissionsServiceTest
{
    readonly DateSetFactory _dateSetFactory = new();
    
    [Fact]
    public async void DatePeriod_GetEmissions_EmissionRecordsReturned()
    {
        var result = new Fixture().Create<EmissionsResponse>();
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var serialize = JsonSerializer.Serialize(result, options);
        var edsMock = SetupHttpClient(serialize);

        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var sut = new EnergiDataService(null, edsMock);

        var res = await sut.GetEmissionsPerHour(dateFrom, dateTo);

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
        var measurements = _dateSetFactory.CreateMeasurements();

        var mockDataSyncService = new Mock<IDataSyncService>();

        mockDataSyncService.Setup(a => a.GetMeasurements(It.IsAny<AuthorizationContext>(), It.IsAny<long>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Aggregation>()))
            .Returns(Task.FromResult(measurements.AsEnumerable()));

        var sut = new EmissionsService(mockDataSyncService.Object, null, null);
        //Act

        var timeseries = await sut.GetTimeSeries(context,
            ((DateTimeOffset) DateTime.SpecifyKind(dateFrom, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            ((DateTimeOffset) DateTime.SpecifyKind(dateTo, DateTimeKind.Utc)).ToUnixTimeSeconds(), Aggregation.Hour,
            meteringPoints);
        //Assert

        Assert.NotEmpty(timeseries);
        Assert.Equal(measurements.Count, timeseries.First().Measurements.Count());
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