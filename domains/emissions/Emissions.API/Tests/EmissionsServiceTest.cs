using System;
using System.Net.Http;
using API.Services;
using Moq;
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
        var eds = new HttpClient();
        eds.BaseAddress = new Uri("https://api.energidataservice.dk/");
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        
        var sut = new EnergiDataService(eds);


        var res = await sut.GetEmissions(dateFrom, dateTo, "DK1");
        
        Assert.NotNull(res);
        Assert.NotEmpty(res.Result.EmissionRecords);
    }
}