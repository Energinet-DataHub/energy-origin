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
using RichardSzalay.MockHttp;
using Xunit;

namespace Tests;

public class EmissionsServiceTest
{
    [Fact]
    public async void a()
    {
        var dataSyncMock = new Mock<IDataSyncService>();
       // dataSyncMock.Setup(x => x.GetListOfMeteringPoints().ReturnsAsync());
    }

    [Fact]
    public async void OneDayPeriod_GetEmissions_EmissionRecordsReturned()
    {
        var result = new Fixture().Create<EmissionsResponse>();
        
        var edsMock = SetupHttpClient(JsonSerializer.Serialize(result));
        
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var sut = new EnergiDataService(TODO, edsMock);

        var res = await sut.GetEmissions(dateFrom, dateTo);
        
        Assert.NotNull(res);
        Assert.NotEmpty(res.Result.EmissionRecords);
    }

    [Fact]
    public async void ConsumptionAndEmission_CalculateTotalEmission_TotalEmission()
    {

        //var emissions = CreateEmissions(dateFrom, dateTo);
        //var meteringPoints = CreateMeteringPoints();
        //var measurements = CreateMeasurements(dateFrom, dateTo);
        //var context = new Mock<AuthorizationContext>();

        //var mockHttp = new MockHttpMessageHandler();
        //mockHttp.When("meteringPoints*").Respond("text/json", JsonSerializer.Serialize(meteringPoints));
        //mockHttp.When("measurements*").Respond("text/json", JsonSerializer.Serialize(measurements));
        //mockHttp.When("emissions*").Respond("text/json", JsonSerializer.Serialize(emissions));

        //var edsHttpClientMock = new HttpClient(mockHttp);
        //var dataSyncHttpClientMock = new HttpClient(mockHttp);
        //var edsService = new EnergiDataService(null, edsHttpClientMock);
        //var dataSyncService = new DataSyncService(null, dataSyncHttpClientMock);

        //Arrange
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var meteringPoints = new List<MeteringPoint>();

        var mockDataSyncService = new Mock<IDataSyncService>();
        mockDataSyncService.Setup(a => a.GetListOfMeteringPoints(It.IsAny<AuthorizationContext>()))
            .Returns(Task.FromResult(meteringPoints.AsEnumerable()));

        var mockEdsService = new Mock<IEnergiDataService>();
        //var sut = new EmissionsService(mockDataSyncService.Object, mockEdsService.Object);
        var mockSut = new Mock<EmissionsService>();
        mockSut.Setup(a => a.GetEmissions(It.IsAny<AuthorizationContext>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<Aggregation>()))
            .CallBase();
        mockSut.Setup(a => a.CalculateTotalEmission(It.IsAny<List<EmissionRecord>>(), It.IsAny<List<Tuple<MeteringPoint, IEnumerable<Measurement>>>>(), It.IsAny<long>(), It.IsAny<long>()));

        //Act

        //Assert



        var emissionsResult = sut.GetEmissions(context.Object, ((DateTimeOffset)DateTime.SpecifyKind(dateFrom, DateTimeKind.Utc)).ToUnixTimeSeconds(), ((DateTimeOffset)DateTime.SpecifyKind(dateTo, DateTimeKind.Utc)).ToUnixTimeSeconds() , Aggregation.Hour);
    }

    private HttpClient SetupHttpClient(string serialize)
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