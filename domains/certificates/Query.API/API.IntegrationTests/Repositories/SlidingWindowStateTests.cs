using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using API.Models;
using API.UnitTests;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace API.IntegrationTests.Repositories;

[Collection(IntegrationTestCollection.CollectionName)]
public class SlidingWindowStateTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public SlidingWindowStateTests(IntegrationTestFixture integrationTestFixture)
    {
        var databaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().GetAwaiter().GetResult();
        new DbMigrator(databaseInfo.ConnectionString, typeof(ApplicationDbContext).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(databaseInfo.ConnectionString).Options;
    }

    private static MeteringPointSyncInfo CreateSyncInfo(Gsrn gsrn) =>
        new(
            Gsrn: gsrn,
            StartSyncDate: DateTimeOffset.Now.AddDays(-1),
            EndSyncDate: null,
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

        actualPeriodStartTime.SynchronizationPoint.EpochSeconds.Should().Be(UnixTimestamp.Create(info.StartSyncDate).RoundToNextHour().EpochSeconds);
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

        actualPeriodStartTime.SynchronizationPoint.EpochSeconds.Should().Be(UnixTimestamp.Create(info.StartSyncDate).RoundToNextHour().EpochSeconds);
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

    [Fact]
    public async Task BugWithEFCoreJsonBColumnThrowsExceptionOnSaveChanges()
    {
        var now = UnixTimestamp.Create(DateTimeOffset.Parse("2024-12-06T16:00:05.7527390+00:00"))
            .RoundToLatestHour(); // 2024-12-06T16:00:05.7527390+00:00

        var gsrn = Any.Gsrn();
        var missingIntervalStart = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-25 05:00:00 +00:00")); // 2024-11-29T00:00:00.0000000+00:00
        var missingIntervalEnd = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-28 05:00:00 +00:00")); // 11/28/2024 05:00:00 +00:00
        var syncPoint = UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-28 23:00:00 +00:00")); // 11/28/2024 23:00:00 +00:00
        var missingInterval = new List<MeasurementInterval>(new[] { MeasurementInterval.Create(missingIntervalStart, missingIntervalEnd) });
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, syncPoint, missingInterval);

        await using var dbContext = new ApplicationDbContext(_options);
        dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);
        await dbContext.SaveChangesAsync();

        List<Measurement> measurements =
        [
            CreateMeasurement(gsrn, UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-28T23:00:00.0000000+00:00")).EpochSeconds,
                UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T00:00:00.0000000+00:00")).EpochSeconds, 11),

            CreateMeasurement(gsrn, UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T00:00:00.0000000+00:00")).EpochSeconds,
                UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T01:00:00.0000000+00:00")).EpochSeconds, 22),

            CreateMeasurement(gsrn, UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T01:00:00.0000000+00:00")).EpochSeconds,
                UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T02:00:00.0000000+00:00")).EpochSeconds, 33),

            CreateMeasurement(gsrn, UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T02:00:00.0000000+00:00")).EpochSeconds,
                UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T03:00:00.0000000+00:00")).EpochSeconds, 44),

            CreateMeasurement(gsrn, UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T03:00:00.0000000+00:00")).EpochSeconds,
                UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T04:00:00.0000000+00:00")).EpochSeconds, 55),

            CreateMeasurement(gsrn, UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T04:00:00.0000000+00:00")).EpochSeconds,
                UnixTimestamp.Create(DateTimeOffset.Parse("2024-11-29T05:00:00.0000000+00:00")).EpochSeconds, 66),
        ];

        await using var newDbContext = new ApplicationDbContext(_options);
        var fetchedWindow = await newDbContext.MeteringPointTimeSeriesSlidingWindows.FirstAsync(CancellationToken.None);
        var service = new SlidingWindowService(new MeasurementSyncMetrics());
        var stateRepo = new SlidingWindowState(newDbContext);
        var newSyncPosition = UnixTimestamp.Create(DateTimeOffset.Parse("2024-12-06T16:00:05.9689256+00:00")); // 2024-12-06T16:00:05.9689256+00:00
        service.UpdateSlidingWindow(fetchedWindow, measurements, newSyncPosition);
        await stateRepo.UpsertSlidingWindow(fetchedWindow, CancellationToken.None);
        await stateRepo.SaveChangesAsync(CancellationToken.None); // Used to throw exception in EF Core
    }

    private Measurement CreateMeasurement(Gsrn gsrn, long from, long to, long quantity)
    {
        return new Measurement
        {
            Quality = EnergyQuality.Measured,
            DateFrom = from,
            DateTo = to,
            Gsrn = gsrn.Value,
            Quantity = quantity
        };
    }

}
