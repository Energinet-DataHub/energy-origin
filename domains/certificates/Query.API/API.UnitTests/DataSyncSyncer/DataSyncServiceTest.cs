using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Persistence;
using API.MasterDataService;
using CertificateEvents.Primitives;
using IntegrationEvents;
using Microsoft.Extensions.Logging;
using Moq;
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

    private readonly Mock<IDataSyncClient> fakeClient = new();
    private readonly Mock<ILogger<DataSyncService>> fakeLogger = new();

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
        fakeClient.Setup(it => it.RequestAsync(
                validMasterData.GSRN,
                It.IsAny<Period>(),
                validMasterData.MeteringPointOwner,
                CancellationToken.None)
            )
            .ReturnsAsync(() => fakeResponseList);

        var service = SetupService(meteringPointOnboarded);

        var response = await service.FetchMeasurements(validMasterData.GSRN, validMasterData.MeteringPointOwner,
            CancellationToken.None);

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

    [Fact]
    public async Task GetMeasurements_MeteringPointOnboarded_NoDataFetched()
    {
        var meteringPointOnboarded = DateTimeOffset.Now.AddDays(-1);
        fakeClient.Setup(it => it.RequestAsync(
                validMasterData.GSRN,
                It.IsAny<Period>(),
                validMasterData.MeteringPointOwner,
                CancellationToken.None)
            )
            .ReturnsAsync(() => new List<DataSyncDto>());

        var service = SetupService(meteringPointOnboarded);

        var response = await service.FetchMeasurements(validMasterData.GSRN, validMasterData.MeteringPointOwner,
            CancellationToken.None);

        Assert.Empty(response);
        fakeClient.Verify(
            c => c.RequestAsync(It.IsAny<string>(), It.IsAny<Period>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMeasurements_MeteringPointNotOnboarded_NoDataFetched()
    {
        var meteringPointOnboarded = DateTimeOffset.Now.AddDays(1);

        var service = SetupService(meteringPointOnboarded);

        var response = await service.FetchMeasurements(validMasterData.GSRN, validMasterData.MeteringPointOwner,
            CancellationToken.None);

        Assert.Empty(response);
        fakeClient.Verify(
            c => c.RequestAsync(It.IsAny<string>(), It.IsAny<Period>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    private DataSyncService SetupService(DateTimeOffset meteringPointOnboardedStartDate)
    {
        Mock<ISyncState> fakeState = new();
        fakeState.Setup(s => s.GetPeriodStartTime(validMasterData.GSRN))
            .Returns(meteringPointOnboardedStartDate.ToUnixTimeSeconds);

        var service = new DataSyncService(
            client: fakeClient.Object,
            logger: fakeLogger.Object,
            syncState: fakeState.Object
        );

        return service;
    }
}
