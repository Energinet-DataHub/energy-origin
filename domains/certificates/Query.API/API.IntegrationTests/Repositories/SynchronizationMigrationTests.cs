using API.DataSyncSyncer.Persistence;
using API.IntegrationTests.Helpers;
using API.IntegrationTests.Testcontainers;
using FluentAssertions;
using Marten;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests.Repositories;

public static class SynchronizationMigration
{
    public static async Task MigrateSynchronizationPosition(this IDocumentStore store, CancellationToken cancellationToken)
    {
        await using var session = store.OpenSession();

        var allPositions = await session.Query<SyncPosition>().ToListAsync(cancellationToken);

        var synchronizationPositions = allPositions
            .GroupBy(p => p.GSRN)
            .Select(g => new SynchronizationPosition {GSRN = g.Key, SyncedTo = g.Max(p => p.SyncedTo)});

        session.Store(synchronizationPositions);

        foreach (var syncPosition in allPositions)
        {
            session.Delete(syncPosition);
        }

        await session.SaveChangesAsync(cancellationToken);
    }
}

public class SynchronizationMigrationTests : IClassFixture<MartenDbContainer>, IAsyncLifetime
{
    private readonly IDocumentStore store;

    public SynchronizationMigrationTests(MartenDbContainer dbContainer)
        => store = DocumentStore.For(opts => opts.Connection(dbContainer.ConnectionString));

    [Fact]
    public async Task migrates_one_metering_point_with_positons()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var sync1 = new SyncPosition(Guid.NewGuid(), gsrn, 42);
        var sync2 = new SyncPosition(Guid.NewGuid(), gsrn, 43);
        var sync3 = new SyncPosition(Guid.NewGuid(), gsrn, 44);

        await SaveSyncPositions(sync1, sync2, sync3);

        await store.MigrateSynchronizationPosition(CancellationToken.None);

        await using var session = store.QuerySession();

        var syncPositions = await session.Query<SyncPosition>().ToListAsync();
        syncPositions.Should().HaveCount(0);

        var synchronizationPositions = await session.Query<SynchronizationPosition>().ToListAsync();
        synchronizationPositions.Should().HaveCount(1);
        synchronizationPositions.Single().Should().BeEquivalentTo(new SynchronizationPosition { GSRN = gsrn, SyncedTo = 44 });
    }

    [Fact]
    public async Task can_be_called_multiple_times()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var sync1 = new SyncPosition(Guid.NewGuid(), gsrn, 42);
        
        await SaveSyncPositions(sync1);

        // Call multiple times
        await store.MigrateSynchronizationPosition(CancellationToken.None);
        await store.MigrateSynchronizationPosition(CancellationToken.None);
        await store.MigrateSynchronizationPosition(CancellationToken.None);

        await using var session = store.QuerySession();

        var syncPositions = await session.Query<SyncPosition>().ToListAsync();
        syncPositions.Should().HaveCount(0);

        var synchronizationPositions = await session.Query<SynchronizationPosition>().ToListAsync();
        synchronizationPositions.Should().HaveCount(1);
        synchronizationPositions.Single().Should().BeEquivalentTo(new SynchronizationPosition { GSRN = gsrn, SyncedTo = 42 });
    }

    [Fact]
    public async Task migrates_20_metering_points_with_1_year_positions()
    {
        const int hoursInAYear = 24 * 365;
        const int numMeteringPoints = 20;

        var gsrns = Enumerable.Range(1, numMeteringPoints).Select(_ => GsrnHelper.GenerateRandom()).ToArray();

        int i = 0;
        foreach (var gsrn in gsrns)
        {
            var positionsForMeteringPoint = Enumerable.Range(1, hoursInAYear)
                .Select(h => new SyncPosition(Guid.NewGuid(), gsrn, h+i))
                .ToArray();

            await SaveSyncPositions(positionsForMeteringPoint);
            i++;
        }

        await store.MigrateSynchronizationPosition(CancellationToken.None);

        await using var session = store.QuerySession();

        var syncPositions = await session.Query<SyncPosition>().ToListAsync();
        syncPositions.Should().HaveCount(0);

        var synchronizationPositions = await session.Query<SynchronizationPosition>().ToListAsync();
        var expected = gsrns.Select((gsrn, cnt) => new SynchronizationPosition { GSRN = gsrn, SyncedTo = hoursInAYear + cnt });
        synchronizationPositions.Should().BeEquivalentTo(expected);
    }


    private async Task SaveSyncPositions(params SyncPosition[] positions)
    {
        await using var session = store.OpenSession();

        session.Store(positions);
        await session.SaveChangesAsync();
    }

    private async Task DeleteAll()
    {
        await using var session = store.OpenSession();
        var allSyncPositions = await session.Query<SyncPosition>().ToListAsync();

        foreach (var syncPosition in allSyncPositions)
        {
            session.Delete(syncPosition);
        }

        var allSynchronizationPositions = await session.Query<SynchronizationPosition>().ToListAsync();

        foreach (var synchronizationPosition in allSynchronizationPositions)
        {
            session.Delete(synchronizationPosition);
        }

        await session.SaveChangesAsync();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DeleteAll();
        store.Dispose();
    }
}
