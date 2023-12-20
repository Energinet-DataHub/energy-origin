using API.DataSyncSyncer;
using API.DataSyncSyncer.Persistence;
using API.IntegrationTests.Mocks;
using DataContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Threading.Tasks;
using DataContext.Models;
using Testing.Helpers;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class SyncStateTests : IClassFixture<DbContextFactoryMock>
{
    private readonly IDbContextFactory<ApplicationDbContext> factory;

    public SyncStateTests(DbContextFactoryMock mock) => factory = mock;

    private static MeteringPointSyncInfo CreateSyncInfo(string? gsrn = null) =>
        new(
            GSRN: gsrn ?? GsrnHelper.GenerateRandom(),
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

    [Fact]
    public async Task SetSyncPosition_FirstTime_ReturnsSyncedTo()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var syncedTo = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var syncState = new SyncState(factory, Substitute.For<ILogger<SyncState>>());
        await syncState.SetSyncPosition(gsrn, syncedTo);

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(CreateSyncInfo(gsrn));
        actualPeriodStartTime.Should().Be(syncedTo);
    }

    [Fact]
    public async Task SetSyncPosition_SecondTime_ReturnsSecondSyncPosition()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var syncedTo1 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var syncedTo2 = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds();

        var syncState = new SyncState(factory, Substitute.For<ILogger<SyncState>>());
        await syncState.SetSyncPosition(gsrn, syncedTo1);
        await syncState.SetSyncPosition(gsrn, syncedTo2);

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(CreateSyncInfo(gsrn));
        actualPeriodStartTime.Should().Be(syncedTo2);
    }
}
