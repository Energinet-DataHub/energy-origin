using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Dto;
using API.MasterDataService;
using CertificateEvents.Primitives;
using IntegrationEvents;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using Xunit;

namespace API.DataSyncSyncer;

public class DataSyncServiceTest
{
    private readonly MasterData validMasterData = new(
        GSRN: "gsrn",
        GridArea: "gridArea",
        Type: MeteringPointType.Production,
        Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointOnboardedStartDate: DateTimeOffset.Now.AddDays(-1));

    private readonly Mock<ILogger<DataSyncService>> fakeLogger = new();
    private readonly MockHttpMessageHandler fakeHttpClient = new();

    [Fact]
    public async Task GetMeasurements_MeteringPointOnboarded_DataFetched()
    {
        var meteringPointOnboarded = DateTimeOffset.Now.AddDays(-1);
        var fakeResponseList = new List<DataSyncDto>
        {
            new(
                GSRN: validMasterData.GSRN,
                DateFrom: meteringPointOnboarded.ToUnixTimeSeconds(),
                DateTo: DateTimeOffset.Now.ToUnixTimeSeconds(),
                Quantity: 5,
                Quality: MeasurementQuality.Measured
            )
        };

        var service = SetupService(meteringPointOnboarded);

        fakeHttpClient
            .Expect("/measurements")
            .WithQueryString("gsrn", validMasterData.GSRN)
            .Respond("application/json", JsonConvert.SerializeObject(fakeResponseList));

        await service.FetchMeasurements(validMasterData.GSRN, validMasterData.MeteringPointOwner,
            CancellationToken.None);
        fakeHttpClient.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetMeasurements_MeteringPointOnboarded_DateFromUpdated()
    {
        var meteringPointOnboarded = DateTimeOffset.Now.AddDays(-2);
        var service = SetupService(meteringPointOnboarded);

        await AssertDateFromQueryParamIsUpdated(meteringPointOnboarded, service);
    }

    [Fact]
    public async Task GetMeasurements_MeteringPointNotOnboarded_NoDataFetched()
    {
        var meteringPointOnboarded = DateTimeOffset.Now.AddDays(1);

        var service = SetupService(meteringPointOnboarded);

        var response = await service.FetchMeasurements(validMasterData.GSRN, validMasterData.MeteringPointOwner,
            CancellationToken.None);

        Assert.Empty(response);
    }

    [Fact]
    public async Task GetMeasurements_ErrorFromDatahub_NoDataFetched()
    {
        var meteringPointOnboarded = DateTimeOffset.Now.AddDays(-1);

        var service = SetupService(meteringPointOnboarded);

        fakeHttpClient
            .Expect("/measurements")
            .WithQueryString("gsrn", validMasterData.GSRN)
            .Respond(HttpStatusCode.InternalServerError);

        var response = await service.FetchMeasurements(validMasterData.GSRN, validMasterData.MeteringPointOwner,
            CancellationToken.None);

        fakeHttpClient.VerifyNoOutstandingExpectation();

        Assert.Empty(response);
    }

    private async Task AssertDateFromQueryParamIsUpdated(DateTimeOffset meteringPointOnboarded, DataSyncService service)
    {
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

        fakeHttpClient
            .Expect("/measurements")
            .WithQueryString("gsrn", validMasterData.GSRN)
            .WithQueryString("dateFrom", meteringPointOnboarded.ToUnixTimeSeconds().ToString())
            .Respond("application/json", JsonConvert.SerializeObject(fakeResponseList));

        await service.FetchMeasurements(validMasterData.GSRN, validMasterData.MeteringPointOwner,
            CancellationToken.None);

        fakeHttpClient.VerifyNoOutstandingExpectation();
        fakeHttpClient.Clear();

        fakeHttpClient
            .Expect("/measurements")
            .WithQueryString("gsrn", validMasterData.GSRN)
            .WithQueryString("dateFrom", fakeResponseList[0].DateTo.ToString())
            .Respond("application/json", JsonConvert.SerializeObject(fakeResponseList));
        await service.FetchMeasurements(validMasterData.GSRN, validMasterData.MeteringPointOwner,
            CancellationToken.None);

        fakeHttpClient.VerifyNoOutstandingExpectation();
    }

    private DataSyncService SetupService(DateTimeOffset meteringPointOnboardedStartDate)
    {
        var client = fakeHttpClient.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost:8080");

        var masterDataList = new List<MasterData>
        {
            validMasterData with
            {
                MeteringPointOnboardedStartDate = meteringPointOnboardedStartDate
            }
        };


        var service = new DataSyncService(
            httpClient: client,
            logger: fakeLogger.Object
        );
        var state = masterDataList
            .Where(it => !string.IsNullOrWhiteSpace(it.GSRN))
            .ToDictionary(m => m.GSRN, m => m.MeteringPointOnboardedStartDate);

        service.SetState(state);

        return service;
    }
}
