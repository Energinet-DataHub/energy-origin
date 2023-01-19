using System;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.DataSyncSyncer.Persistence;
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
    private readonly CertificateIssuingContract contract = new()
    {
        GSRN = "gsrn",
        GridArea = "gridArea",
        MeteringPointType = MeteringPointType.Production,
        MeteringPointOwner = "meteringPointOwner",
        StartDate = DateTimeOffset.Now.AddDays(-1)
    };

    [Fact]
    public async Task GetPeriodStartTime_NoDataInStore_ReturnsContractStartDate()
    {
        var storeMock = CreateStoreMock(data: null);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(contract.StartDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStore_ReturnsNewestDate()
    {
        var view = new CertificatesByOwnerView { Owner = contract.MeteringPointOwner };

        var certDateFrom = contract.StartDate.ToUnixTimeSeconds();
        var certDateTo = contract.StartDate.AddHours(1).ToUnixTimeSeconds();

        view.Certificates.Add(Guid.NewGuid(),
            new CertificateView { DateFrom = certDateFrom, DateTo = certDateTo, GSRN = contract.GSRN });

        var storeMock = CreateStoreMock(view);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(certDateTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStoreButIsBeforeContractStartDate_ReturnsContractStartDate()
    {
        var view = new CertificatesByOwnerView { Owner = contract.MeteringPointOwner };

        view.Certificates.Add(Guid.NewGuid(),
            new CertificateView { DateFrom = 1000, DateTo = 2000, GSRN = contract.GSRN });

        var storeMock = CreateStoreMock(view);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(contract.StartDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_TwoCertificatesInStore_ReturnsNewestDate()
    {
        var view = new CertificatesByOwnerView { Owner = contract.MeteringPointOwner };

        var cert1DateFrom = contract.StartDate.ToUnixTimeSeconds();
        var cert1DateTo = contract.StartDate.AddHours(1).ToUnixTimeSeconds();

        view.Certificates.Add(Guid.NewGuid(), new CertificateView { DateFrom = cert1DateFrom, DateTo = cert1DateTo, GSRN = contract.GSRN });

        var cert2DateFrom = contract.StartDate.AddHours(1).ToUnixTimeSeconds();
        var cert2DateTo = contract.StartDate.AddHours(2).ToUnixTimeSeconds();

        view.Certificates.Add(Guid.NewGuid(), new CertificateView { DateFrom = cert2DateFrom, DateTo = cert2DateTo, GSRN = contract.GSRN });

        var storeMock = CreateStoreMock(view);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(cert2DateTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_NoCertificatesForThatGsrnInStore_ReturnsContractStartDate()
    {
        var view = new CertificatesByOwnerView { Owner = contract.MeteringPointOwner };

        var certDateFrom = contract.StartDate.ToUnixTimeSeconds();
        var certDateTo = contract.StartDate.AddHours(1).ToUnixTimeSeconds();

        view.Certificates.Add(Guid.NewGuid(), new CertificateView { DateFrom = certDateFrom, DateTo = certDateTo, GSRN = "someOtherGsrn" });

        var storeMock = CreateStoreMock(view);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(contract.StartDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_DatabaseCommunicationFailure_ReturnsNull()
    {
        var storeMock = new Mock<IDocumentStore>();
        storeMock.Setup(m => m.QuerySession())
            .Throws<Exception>()
            .Verifiable();

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

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
