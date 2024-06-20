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
using Microsoft.Extensions.Logging;
using NSubstitute;
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
    public async Task GetPeriodStartTime_NoDataInStore_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo();

        await using var dbContext = new ApplicationDbContext(options);
        var syncState = new SyncState(dbContext, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info, CancellationToken.None);

        actualPeriodStartTime.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStore_ReturnsNewestDate()
    {
        var info = CreateSyncInfo();

        var position = new SynchronizationPosition { GSRN = info.GSRN, SyncedTo = DateTimeOffset.Now.ToUnixTimeSeconds() };

        await using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Add(position);
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = new ApplicationDbContext(options);
        var syncState = new SyncState(newDbContext, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info, CancellationToken.None);

        actualPeriodStartTime.Should().Be(position.SyncedTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStoreButIsBeforeContractStartDate_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo();

        var position = new SynchronizationPosition { GSRN = info.GSRN, SyncedTo = info.StartSyncDate.AddHours(-1).ToUnixTimeSeconds() };

        await using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Add(position);
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = new ApplicationDbContext(options);
        var syncState = new SyncState(newDbContext, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info, CancellationToken.None);

        actualPeriodStartTime.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
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
