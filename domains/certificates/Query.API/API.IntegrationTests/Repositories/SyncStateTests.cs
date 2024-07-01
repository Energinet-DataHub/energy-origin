using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Persistence;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testing.Helpers;
using Xunit;

namespace API.IntegrationTests.Repositories;

[Collection(IntegrationTestCollection.CollectionName)]
public class SyncStateTests
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public SyncStateTests(IntegrationTestFixture integrationTestFixture)
    {
        var emptyDb = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(emptyDb.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
    }

    private static MeteringPointSyncInfo CreateSyncInfo(string? gsrn = null) =>
        new(
            GSRN: gsrn ?? GsrnHelper.GenerateRandom(),
            StartSyncDate: DateTimeOffset.Now.AddDays(-1),
            MeteringPointOwner: "SomeMeteringPointOwner");

    [Fact]
    public async Task GetSlidingWindowStartTime_NoDataInStore_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo();

        await using var dbContext = new ApplicationDbContext(options);
        var syncState = new SyncState(dbContext);

        var actualPeriodStartTime = await syncState.GetSlidingWindowStartTime(info, CancellationToken.None);

        actualPeriodStartTime.SynchronizationPoint.Seconds.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetSlidingWindowStartTime_SlidingWindowInStore_ReturnsNewestDate()
    {
        var info = CreateSyncInfo();

        var position = MeteringPointTimeSeriesSlidingWindow.Create(info.GSRN, UnixTimestamp.Create(DateTimeOffset.Now.ToUnixTimeSeconds()));

        await using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(position);
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = new ApplicationDbContext(options);
        var syncState = new SyncState(newDbContext);

        var actualPeriodStartTime = await syncState.GetSlidingWindowStartTime(info, CancellationToken.None);

        actualPeriodStartTime.SynchronizationPoint.Should().Be(position.SynchronizationPoint);
    }

    [Fact]
    public async Task GetSlidingWindowStartTime_SlidingWindowInStoreButIsBeforeContractStartDate_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo();

        var position = MeteringPointTimeSeriesSlidingWindow.Create(info.GSRN, UnixTimestamp.Create(DateTimeOffset.Now.AddDays(-2).ToUnixTimeSeconds()));

        await using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(position);
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = new ApplicationDbContext(options);
        var syncState = new SyncState(newDbContext);

        var actualPeriodStartTime = await syncState.GetSlidingWindowStartTime(info, CancellationToken.None);

        actualPeriodStartTime.SynchronizationPoint.Seconds.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GivenSlidingWindow_WhenStoringInDatabase_MissingIntervalsCanBeRetrievedLater()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var missingIntervalStart = UnixTimestamp.Now().Add(TimeSpan.FromHours(-2));
        var missingIntervalEnd = UnixTimestamp.Now().Add(TimeSpan.FromHours(-1));
        var missingInterval = new List<MeasurementInterval>(new[] { MeasurementInterval.Create(missingIntervalStart, missingIntervalEnd) });
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, UnixTimestamp.Now(), missingInterval);

        await using var dbContext = new ApplicationDbContext(options);
        dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);
        await dbContext.SaveChangesAsync();

        await using var newDbContext = new ApplicationDbContext(options);
        var fetchedSlidingWindow = await newDbContext.MeteringPointTimeSeriesSlidingWindows.FirstAsync();

        Assert.Single(fetchedSlidingWindow.MissingMeasurements.Intervals);
        Assert.Equal(missingInterval.First().From, fetchedSlidingWindow.MissingMeasurements.Intervals[0].From);
        Assert.Equal(missingInterval.First().To, fetchedSlidingWindow.MissingMeasurements.Intervals[0].To);
    }
}
