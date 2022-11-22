using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Persistence;
using API.MasterDataService;
using CertificateEvents.Primitives;
using FluentAssertions;
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
    private readonly Mock<ISyncStateFactory> fakeSyncStateFactory = new();

    [Fact]
    public async Task FetchMeasurements_MeteringPointOnboarded_DataFetched()
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
        response.Should().Equal(fakeResponseList);
    }

    [Fact]
    public async Task FetchMeasurements_MeteringPointOnboarded_NoDataFetched()
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
    public async Task FetchMeasurements_MeteringPointNotOnboarded_NoDataFetched()
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
        var fakeState = new Dictionary<string, DateTimeOffset>
        {
            { validMasterData.GSRN, meteringPointOnboardedStartDate }
        };

        fakeSyncStateFactory
            .Setup(it => it.CreateSyncState(It.IsAny<Dictionary<string, DateTimeOffset>>())).r
            .Returns(new SyncState(fakeState));

        var service = new DataSyncService(
            client: fakeClient.Object,
            logger: fakeLogger.Object,
            syncStateFactory: fakeSyncStateFactory.Object
        );

        service.SetState(fakeState);

        return service;
    }
}
