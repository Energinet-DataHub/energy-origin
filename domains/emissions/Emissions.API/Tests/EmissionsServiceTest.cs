using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Services;
using AutoFixture;
using Moq;
using Moq.Protected;
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
    public async void GetEmissions()
    {
        var edsMock = SetupHttpClient();
        
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var sut = new EnergiDataService(edsMock);

        var res = await sut.GetEmissions(dateFrom, dateTo);
        
        Assert.NotNull(res);
        Assert.NotEmpty(res.Result.EmissionRecords);
    }

    private HttpClient SetupHttpClient()
    {
        var result = new Fixture().Create<EmissionsResponse>();
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
                Content = new StringContent(JsonSerializer.Serialize(result)),
            }).Verifiable();
        
        
        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test.com/"),
        };
    }
}