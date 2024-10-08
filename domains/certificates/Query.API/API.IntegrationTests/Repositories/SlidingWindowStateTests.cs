using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Persistence;
using API.UnitTests;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.Repositories;

[Collection(IntegrationTestCollection.CollectionName)]
public class SlidingWindowStateTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public SlidingWindowStateTests(IntegrationTestFixture integrationTestFixture)
    {
        var emptyDb = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(emptyDb.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(_options);
        dbContext.Database.EnsureCreated();
    }

    private static MeteringPointSyncInfo CreateSyncInfo(Gsrn gsrn) =>
        new(
            Gsrn: gsrn,
            StartSyncDate: DateTimeOffset.Now.AddDays(-1),
            MeteringPointOwner: "SomeMeteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));

    [Fact]
    public async Task GetSlidingWindowStartTime_NoDataInStore_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo(Any.Gsrn());

        await using var dbContext = new ApplicationDbContext(_options);
        var syncState = new SlidingWindowState(dbContext);

        var actualPeriodStartTime = await syncState.GetSlidingWindowStartTime(info, CancellationToken.None);

        actualPeriodStartTime.SynchronizationPoint.Seconds.Should().Be(UnixTimestamp.Create(info.StartSyncDate).RoundToNextHour().Seconds);
    }

    [Fact]
    public async Task GetSlidingWindowStartTime_SlidingWindowInStore_ReturnsNewestDate()
    {
        var info = CreateSyncInfo(Any.Gsrn());

        var position = MeteringPointTimeSeriesSlidingWindow.Create(info.Gsrn, UnixTimestamp.Create(DateTimeOffset.Now.ToUnixTimeSeconds()));

        await using (var dbContext = new ApplicationDbContext(_options))
        {
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(position);
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = new ApplicationDbContext(_options);
        var syncState = new SlidingWindowState(newDbContext);

        var actualPeriodStartTime = await syncState.GetSlidingWindowStartTime(info, CancellationToken.None);

        actualPeriodStartTime.SynchronizationPoint.Should().Be(position.SynchronizationPoint);
    }

    [Fact]
    public async Task GetSlidingWindowStartTime_SlidingWindowInStoreButIsBeforeContractStartDate_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo(Any.Gsrn());

        var position = MeteringPointTimeSeriesSlidingWindow.Create(info.Gsrn, UnixTimestamp.Create(DateTimeOffset.Now.AddDays(-2).ToUnixTimeSeconds()));

        await using (var dbContext = new ApplicationDbContext(_options))
        {
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(position);
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = new ApplicationDbContext(_options);
        var syncState = new SlidingWindowState(newDbContext);

        var actualPeriodStartTime = await syncState.GetSlidingWindowStartTime(info, CancellationToken.None);

        actualPeriodStartTime.SynchronizationPoint.Seconds.Should().Be(UnixTimestamp.Create(info.StartSyncDate).RoundToNextHour().Seconds);
    }

    [Fact]
    public async Task GivenSlidingWindow_WhenStoringInDatabase_MissingIntervalsCanBeRetrievedLater()
    {
        var gsrn = Any.Gsrn();
        var missingIntervalStart = UnixTimestamp.Now().Add(TimeSpan.FromHours(-2));
        var missingIntervalEnd = UnixTimestamp.Now().Add(TimeSpan.FromHours(-1));
        var missingInterval = new List<MeasurementInterval>(new[] { MeasurementInterval.Create(missingIntervalStart, missingIntervalEnd) });
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, UnixTimestamp.Now(), missingInterval);

        await using var dbContext = new ApplicationDbContext(_options);
        dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);
        await dbContext.SaveChangesAsync();

        await using var newDbContext = new ApplicationDbContext(_options);
        var fetchedSlidingWindow = await newDbContext.MeteringPointTimeSeriesSlidingWindows.FirstAsync();

        Assert.Single(fetchedSlidingWindow.MissingMeasurements.Intervals);
        Assert.Equal(missingInterval.First().From, fetchedSlidingWindow.MissingMeasurements.Intervals[0].From);
        Assert.Equal(missingInterval.First().To, fetchedSlidingWindow.MissingMeasurements.Intervals[0].To);
    }
}
