using System;
using System.Threading.Tasks;
using API.DataSyncSyncer;
using API.DataSyncSyncer.Persistence;
using API.IntegrationTests.Helpers;
using API.IntegrationTests.Testcontainers;
using FluentAssertions;
using Marten;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class SyncStateTests : IClassFixture<MartenDbContainer>
{
    private readonly MartenDbContainer martenDbContainer;

    public SyncStateTests(MartenDbContainer martenDbContainer) =>
        this.martenDbContainer = martenDbContainer;

    private static MeteringPointSyncInfo CreateSyncInfo() =>
        new(
            GSRN: GsrnHelper.GenerateRandom(),
            StartSyncDate: DateTimeOffset.Now.AddDays(-1),
            MeteringPointOwner: "SomeMeteringPointOwner");

    [Fact]
    public async Task GetPeriodStartTime_NoDataInStore_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo();

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });

        var syncState = new SyncState(store, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStore_ReturnsNewestDate()
    {
        var info = CreateSyncInfo();

        var position = new SyncPosition(Guid.NewGuid(), info.GSRN, DateTimeOffset.Now.ToUnixTimeSeconds());

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });
        await using var session = store.LightweightSession();
        session.Events.Append(Guid.NewGuid(), position);
        await session.SaveChangesAsync();

        var syncState = new SyncState(store, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(position.SyncedTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStoreButIsBeforeContractStartDate_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo();

        var position = new SyncPosition(Guid.NewGuid(), info.GSRN, info.StartSyncDate.AddHours(-1).ToUnixTimeSeconds());

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });
        await using var session = store.LightweightSession();
        session.Events.Append(Guid.NewGuid(), position);
        await session.SaveChangesAsync();

        var syncState = new SyncState(store, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_DatabaseCommunicationFailure_ReturnsNull()
    {
        var info = CreateSyncInfo();

        var storeMock = Substitute.For<IDocumentStore>();
        storeMock.QuerySession().ThrowsForAnyArgs<Exception>();

        var syncState = new SyncState(storeMock, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(null);
        storeMock.Received(1).QuerySession();
    }
}
