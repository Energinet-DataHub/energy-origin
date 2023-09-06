using API.DataSyncSyncer;
using API.DataSyncSyncer.Persistence;
using API.IntegrationTests.Helpers;
using API.IntegrationTests.Testcontainers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using API.Data;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class SyncStateTests : IClassFixture<PostgresContainer>, IDisposable
{
    private readonly IDbContextFactory<ApplicationDbContext> factory;
    private readonly ConcurrentBag<ApplicationDbContext?> disposableContexts = new();

    public SyncStateTests(PostgresContainer dbContainer)
    {
        factory = Substitute.For<IDbContextFactory<ApplicationDbContext>>();

        factory.CreateDbContextAsync().Returns(_ =>
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;
            var dbContext = new ApplicationDbContext(options);
            dbContext.Database.EnsureCreated();
            disposableContexts.Add(dbContext);
            return dbContext;
        });
    }

    private static MeteringPointSyncInfo CreateSyncInfo() =>
        new(
            GSRN: GsrnHelper.GenerateRandom(),
            StartSyncDate: DateTimeOffset.Now.AddDays(-1),
            MeteringPointOwner: "SomeMeteringPointOwner");

    [Fact]
    public async Task GetPeriodStartTime_NoDataInStore_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo();

        var syncState = new SyncState(factory, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStore_ReturnsNewestDate()
    {
        var info = CreateSyncInfo();

        var position = new SynchronizationPosition { GSRN = info.GSRN, SyncedTo = DateTimeOffset.Now.ToUnixTimeSeconds() };

        using (var dbContext = await factory.CreateDbContextAsync())
        {
            dbContext.Add(position);
            await dbContext.SaveChangesAsync();
        }

        var syncState = new SyncState(factory, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(position.SyncedTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStoreButIsBeforeContractStartDate_ReturnsContractStartDate()
    {
        var info = CreateSyncInfo();

        var position = new SynchronizationPosition { GSRN = info.GSRN, SyncedTo = info.StartSyncDate.AddHours(-1).ToUnixTimeSeconds() };

        using (var dbContext = await factory.CreateDbContextAsync())
        {
            dbContext.Add(position);
            await dbContext.SaveChangesAsync();
        }

        var syncState = new SyncState(factory, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(info.StartSyncDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_DatabaseCommunicationFailure_ReturnsNull()
    {
        var info = CreateSyncInfo();

        var factoryMock = Substitute.For<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.CreateDbContextAsync().ThrowsForAnyArgs<Exception>();

        var syncState = new SyncState(factoryMock, Substitute.For<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(info);

        actualPeriodStartTime.Should().Be(null);
        await factoryMock.Received(1).CreateDbContextAsync();
    }

    public void Dispose()
    {
        foreach (var dbContext in disposableContexts)
        {
            dbContext?.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
