using System;
using System.Threading.Tasks;
using API.DataSyncSyncer;
using API.DataSyncSyncer.Persistence;
using API.IntegrationTests.Testcontainers;
using FluentAssertions;
using Marten;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class SyncStateTests :
    IClassFixture<MartenDbContainer>
{
    private readonly MartenDbContainer martenDbContainer;

    private readonly MeteringPointSyncInfo syncInfo = new(
        GSRN: "1234",
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        MeteringPointOwner: "SomeMeteringPointOwner");

    public SyncStateTests(MartenDbContainer martenDbContainer)
    {
        this.martenDbContainer = martenDbContainer;
    }

    [Fact]
    public async Task GetPeriodStartTime_NoDataInStore_ReturnsContractStartDate()
    {
        var info = syncInfo with { GSRN = "1234" };

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });

        var syncState = new SyncState(store, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStore_ReturnsNewestDate()
    {
        var info = syncInfo with { GSRN = "1235" };

        var position = new SyncPosition(Guid.NewGuid(), info.GSRN, DateTimeOffset.Now.ToUnixTimeSeconds());

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });
        await using var session = store.LightweightSession();
        session.Events.Append(Guid.NewGuid(), position);
        await session.SaveChangesAsync();

        var syncState = new SyncState(store, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(position.SyncedTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStoreButIsBeforeContractStartDate_ReturnsContractStartDate()
    {
        var info = syncInfo with { GSRN = "1236" };

        var position = new SyncPosition(Guid.NewGuid(), info.GSRN, info.StartSyncDate.AddHours(-1).ToUnixTimeSeconds());

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });
        await using var session = store.LightweightSession();
        session.Events.Append(Guid.NewGuid(), position);
        await session.SaveChangesAsync();

        var syncState = new SyncState(store, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_DatabaseCommunicationFailure_ReturnsNull()
    {
        var info = syncInfo with { GSRN = "1237" };

        var storeMock = new Mock<IDocumentStore>();
        storeMock.Setup(m => m.QuerySession())
            .Throws<Exception>()
            .Verifiable();

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(null);
        storeMock.Verify();
    }
}
