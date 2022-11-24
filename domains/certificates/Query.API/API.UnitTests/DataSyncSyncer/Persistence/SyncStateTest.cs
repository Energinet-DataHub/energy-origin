using System;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Persistence;
using API.MasterDataService;
using API.Query.API.Projections;
using CertificateEvents.Primitives;
using FluentAssertions;
using Marten;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.UnitTests.DataSyncSyncer.Persistence;

public class SyncStateTest
{
    private readonly MasterData masterData = new(
        GSRN: "gsrn",
        GridArea: "gridArea",
        Type: MeteringPointType.Production,
        Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointOnboardedStartDate: DateTimeOffset.Now.AddDays(-1));


    [Fact]
    public async Task GetPeriodStartTime_NoDataInStore_ReturnsMeteringPointOnboardedStartDate()
    {
        var storeMock = CreateStoreMock(data: null);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(masterData);

        actualPeriodStartTime.Should().Be(masterData.MeteringPointOnboardedStartDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStore_ReturnsNewestDate()
    {
        var view = new CertificatesByOwnerView { Owner = masterData.MeteringPointOwner };

        var start = masterData.MeteringPointOnboardedStartDate;

        var certDateFrom = start.ToUnixTimeSeconds();
        var certDateTo = start.AddHours(1).ToUnixTimeSeconds();

        view.Certificates.Add(Guid.NewGuid(),
            new CertificateView { DateFrom = certDateFrom, DateTo = certDateTo, GSRN = masterData.GSRN });

        var storeMock = CreateStoreMock(view);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(masterData);

        actualPeriodStartTime.Should().Be(certDateTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStoreButIsBeforeOnboardedStartDate_ReturnsMeteringPointOnboardedStartDate()
    {
        var view = new CertificatesByOwnerView { Owner = masterData.MeteringPointOwner };

        view.Certificates.Add(Guid.NewGuid(),
            new CertificateView { DateFrom = 1000, DateTo = 2000, GSRN = masterData.GSRN });

        var storeMock = CreateStoreMock(view);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(masterData);

        actualPeriodStartTime.Should().Be(masterData.MeteringPointOnboardedStartDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_TwoCertificatesInStore_ReturnsNewestDate()
    {
        var view = new CertificatesByOwnerView { Owner = masterData.MeteringPointOwner };

        var start = masterData.MeteringPointOnboardedStartDate;

        var cert1DateFrom = start.ToUnixTimeSeconds();
        var cert1DateTo = start.AddHours(1).ToUnixTimeSeconds();

        view.Certificates.Add(Guid.NewGuid(), new CertificateView { DateFrom = cert1DateFrom, DateTo = cert1DateTo, GSRN = masterData.GSRN });

        var cert2DateFrom = start.AddHours(1).ToUnixTimeSeconds();
        var cert2DateTo = start.AddHours(2).ToUnixTimeSeconds();

        view.Certificates.Add(Guid.NewGuid(), new CertificateView { DateFrom = cert2DateFrom, DateTo = cert2DateTo, GSRN = masterData.GSRN });

        var storeMock = CreateStoreMock(view);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(masterData);

        actualPeriodStartTime.Should().Be(cert2DateTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_NoCertificatesForThatGsrnInStore_ReturnsMeteringPointOnboardedStartDate()
    {
        var view = new CertificatesByOwnerView { Owner = masterData.MeteringPointOwner };

        var start = masterData.MeteringPointOnboardedStartDate;

        var certDateFrom = start.ToUnixTimeSeconds();
        var certDateTo = start.AddHours(1).ToUnixTimeSeconds();

        view.Certificates.Add(Guid.NewGuid(), new CertificateView { DateFrom = certDateFrom, DateTo = certDateTo, GSRN = "someOtherGsrn" });

        var storeMock = CreateStoreMock(view);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(masterData);

        actualPeriodStartTime.Should().Be(masterData.MeteringPointOnboardedStartDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_DatabaseCommunicationFailure_ReturnsNull()
    {
        var storeMock = new Mock<IDocumentStore>();
        storeMock.Setup(m => m.QuerySession())
            .Throws<Exception>()
            .Verifiable();

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(masterData);

        actualPeriodStartTime.Should().Be(null);
        storeMock.Verify();
    }

    private static Mock<IDocumentStore> CreateStoreMock(CertificatesByOwnerView? data)
    {
        var querySessionMock = new Mock<IQuerySession>();
        querySessionMock.Setup(m => m.LoadAsync<CertificatesByOwnerView>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var storeMock = new Mock<IDocumentStore>();
        storeMock.Setup(m => m.QuerySession())
            .Returns(querySessionMock.Object);

        return storeMock;
    }
}
