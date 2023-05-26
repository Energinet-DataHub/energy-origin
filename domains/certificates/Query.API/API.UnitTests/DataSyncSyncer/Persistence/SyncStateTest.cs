using System;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.DataSyncSyncer.Persistence;
using CertificateValueObjects;

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
        var view = new SyncStateView { GSRN = contract.GSRN, SyncDateTo = contract.StartDate.AddHours(1).ToUnixTimeSeconds() };

        var storeMock = CreateStoreMock(view);

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(view.SyncDateTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStoreButIsBeforeContractStartDate_ReturnsContractStartDate()
    {
        var view = new SyncStateView { GSRN = contract.GSRN, SyncDateTo = contract.StartDate.AddHours(-1).ToUnixTimeSeconds() };

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

    private static Mock<IDocumentStore> CreateStoreMock(SyncStateView? data)
    {
        var querySessionMock = new Mock<IQuerySession>();
        querySessionMock.Setup(m => m.LoadAsync<SyncStateView>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var storeMock = new Mock<IDocumentStore>();
        storeMock.Setup(m => m.QuerySession())
            .Returns(querySessionMock.Object);

        return storeMock;
    }
}
