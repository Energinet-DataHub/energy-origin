using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client.Dto;
using API.MasterDataService;
using CertificateEvents.Primitives;
using IntegrationEvents;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using Xunit;

namespace API.DataSyncSyncer.Client;

public class DataSyncClientTest
{
    private readonly MasterData validMasterData = new(
        GSRN: "gsrn",
        GridArea: "gridArea",
        Type: MeteringPointType.Production,
        Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointOnboardedStartDate: DateTimeOffset.Now.AddDays(-1));

    private readonly MockHttpMessageHandler fakeHttpHandler = new();
    private readonly Mock<ILogger<DataSyncClient>> fakeLogger = new();


    public DataSyncClient Setup()
    {
        var client = fakeHttpHandler.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost:8080");

        return new DataSyncClient(
            httpClient: client,
            logger: fakeLogger.Object
        );
    }

    [Fact]
    public async Task GetMeasurements_ErrorFromDatahub_ExceptionIsThrown()
    {
        var meteringPointOnboarded = DateTimeOffset.Now.AddDays(-1);

        fakeHttpHandler
            .Expect("/measurements")
            .WithQueryString("gsrn", validMasterData.GSRN)
            .Respond(HttpStatusCode.InternalServerError);

        var dataSyncClient = Setup();

        await Assert.ThrowsAsync<HttpRequestException>(() => dataSyncClient.RequestAsync(
            GSRN: validMasterData.GSRN,
            period: new Period(
                DateFrom: meteringPointOnboarded.ToUnixTimeSeconds(),
                DateTo: meteringPointOnboarded.AddDays(1).ToUnixTimeSeconds()
            ),
            meteringPointOwner: validMasterData.MeteringPointOwner,
            cancellationToken: CancellationToken.None
        ));

        fakeHttpHandler.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetMeasurements_FromDatahub_DataFetched()
    {
        var meteringPointOnboarded = DateTimeOffset.Now.AddDays(-1);

        var fakeResponseList = new List<DataSyncDto>
        {
            new(
                GSRN: validMasterData.GSRN,
                DateFrom: meteringPointOnboarded.ToUnixTimeSeconds(),
                DateTo: DateTimeOffset.Now.AddDays(-1).ToUnixTimeSeconds(),
                Quantity: 5,
                Quality: MeasurementQuality.Measured
            )
        };

        fakeHttpHandler
            .Expect("/measurements")
            .WithQueryString("gsrn", validMasterData.GSRN)
            .Respond("application/json", JsonConvert.SerializeObject(fakeResponseList));

        var dataSyncClient = Setup();

        var response = await dataSyncClient.RequestAsync(
            GSRN: validMasterData.GSRN,
            period: new Period(meteringPointOnboarded.ToUnixTimeSeconds(),
                meteringPointOnboarded.AddDays(1).ToUnixTimeSeconds()),
            meteringPointOwner: validMasterData.MeteringPointOwner,
            CancellationToken.None
        );

        fakeHttpHandler.VerifyNoOutstandingExpectation();

        Assert.NotEmpty(response);
        Assert.All(response,
            item =>
            {
                Assert.Equal(fakeResponseList[0].GSRN, item.GSRN);
                Assert.Equal(fakeResponseList[0].DateFrom, item.DateFrom);
                Assert.Equal(fakeResponseList[0].DateTo, item.DateTo);
                Assert.Equal(fakeResponseList[0].Quantity, item.Quantity);
                Assert.Equal(fakeResponseList[0].Quality, item.Quality);
            }
        );
    }
}
