using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Configurations;
using API.IntegrationTests.Mocks;
using API.MeasurementsSyncer.Persistence;
using API.UnitTests;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.IntegrationTests.Repositories;

[Collection(IntegrationTestCollection.CollectionName)]
public class ContractStateTest
{
    private readonly ContractState _sut;
    private readonly int _minimumAgeThresholdHours = 5;
    private readonly DbContextFactoryMock _dbContextFactoryMock;

    public ContractStateTest(IntegrationTestFixture integrationTestFixture)
    {
        _dbContextFactoryMock = new DbContextFactoryMock(integrationTestFixture.PostgresContainer);
        var options = Options.Create(new MeasurementsSyncOptions() { MinimumAgeThresholdHours = _minimumAgeThresholdHours });
        _sut = new ContractState(_dbContextFactoryMock, NullLogger<ContractState>.Instance, options);
    }

    [Fact]
    public async Task Bug_GivenContractWithEndDate_WhenFetchingContracts_ActiveContractIsExcluded()
    {
        await using var dbContext = _dbContextFactoryMock.CreateDbContext();
        var startDate = DateTimeOffset.Parse("2024-11-30 23:00:00+00");
        var endDate = DateTimeOffset.Parse("2024-12-31 23:00:00+00");
        var gsrn = new Gsrn("571311111100111111");
        var contract = CertificateIssuingContract.Create(0, gsrn, "DK1", MeteringPointType.Production, "abc",
            startDate, endDate, Guid.NewGuid(), null);
        dbContext.Contracts.Add(contract);

        var syncPoint = UnixTimestamp.Create(new DateTimeOffset(2024, 12, 31, 22, 0, 0, TimeSpan.FromHours(0)));
        var window = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, syncPoint);
        dbContext.MeteringPointTimeSeriesSlidingWindows.Add(window);

        await dbContext.SaveChangesAsync();

        // Should not sync when 'now' is before contract start date
        var syncInfos = await _sut.GetSyncInfos(new DateTimeOffset(2024, 11, 30, 23, 59, 0, TimeSpan.FromHours(1)));
        Assert.Empty(syncInfos);

        // Should sync when 'now' is before contract end date
        syncInfos = await _sut.GetSyncInfos(new DateTimeOffset(2025, 12, 31, 23, 59, 0, TimeSpan.FromHours(1)));
        Assert.Single(syncInfos);

        // Should sync when 'now' is after contract end and contract not 100% synced to the end
        syncInfos = await _sut.GetSyncInfos(new DateTimeOffset(2025, 1, 1, 0, 1, 0, TimeSpan.FromHours(1)));
        Assert.Single(syncInfos);

        // Should sync when 'now' is more than an hour after contract end and contract not 100% synced to the end
        syncInfos = await _sut.GetSyncInfos(new DateTimeOffset(2025, 1, 1, 3, 1, 0, TimeSpan.FromHours(1)));
        Assert.Single(syncInfos);
    }

    [Fact]
    public async Task GivenFullySyncedContract_WhenFetchingContracts_ContractIsExcluded()
    {
        await using var dbContext = _dbContextFactoryMock.CreateDbContext();
        var startDate = DateTimeOffset.Parse("2024-11-30 23:00:00+00");
        var endDate = DateTimeOffset.Parse("2024-12-31 23:00:00+00");
        var gsrn = new Gsrn("571311111100111111");
        var contract = CertificateIssuingContract.Create(0, gsrn, "DK1", MeteringPointType.Production, "abc",
            startDate, endDate, Guid.NewGuid(), null);
        dbContext.Contracts.Add(contract);

        var syncPoint = UnixTimestamp.Create(endDate);
        var window = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, syncPoint);
        dbContext.MeteringPointTimeSeriesSlidingWindows.Add(window);

        await dbContext.SaveChangesAsync();

        // Should exclude fully synced contract
        var syncInfos = await _sut.GetSyncInfos(new DateTimeOffset(2025, 1, 1, 3, 1, 0, TimeSpan.FromHours(1)));
        Assert.Empty(syncInfos);
    }

    [Fact]
    public async Task GivenFullySyncedContractWithMissingInterval_WhenFetchingContracts_ContractIsIncluded()
    {
        await using var dbContext = _dbContextFactoryMock.CreateDbContext();
        var startDate = DateTimeOffset.Parse("2024-11-30 23:00:00+00");
        var endDate = DateTimeOffset.Parse("2024-12-31 23:00:00+00");
        var gsrn = new Gsrn("571311111100111111");
        var contract = CertificateIssuingContract.Create(0, gsrn, "DK1", MeteringPointType.Production, "abc",
            startDate, endDate, Guid.NewGuid(), null);
        dbContext.Contracts.Add(contract);

        var syncPoint = UnixTimestamp.Create(endDate);
        var measurementIntervals = new List<MeasurementInterval>
            { MeasurementInterval.Create(UnixTimestamp.Create(startDate), UnixTimestamp.Create(startDate).AddHours(1)) };
        var window = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, syncPoint, measurementIntervals);
        dbContext.MeteringPointTimeSeriesSlidingWindows.Add(window);

        await dbContext.SaveChangesAsync();

        // Should include contract because of missing interval
        var syncInfos = await _sut.GetSyncInfos(new DateTimeOffset(2025, 1, 1, 3, 1, 0, TimeSpan.FromHours(1)));
        Assert.Single(syncInfos);
    }

    [Theory]
    [InlineData(-4, null, false)]
    [InlineData(2, null, false)]
    [InlineData(-4, -3, false)]
    [InlineData(-6, -4, true)]
    [InlineData(-7, -6, true)]
    public async Task GivenContract_WhenGettingSyncInfo_ContractIsIncludedOrNot(int relativeContractStart, int? relativeContractEnd, bool isIncluded)
    {
        // Given contract
        await using var dbContext = _dbContextFactoryMock.CreateDbContext();
        var gsrn = Any.Gsrn();
        var contractStart = UnixTimestamp.Now().AddHours(relativeContractStart);
        var contractEnd = relativeContractEnd is null ? null : UnixTimestamp.Now().AddHours(relativeContractEnd.Value);
        dbContext.Contracts.Add(Any.CertificateIssuingContract(gsrn, contractStart, contractEnd));
        await dbContext.SaveChangesAsync();

        // When getting sync info
        var syncInfo = (await _sut.GetSyncInfos()).SingleOrDefault(si => si.Gsrn.Equals(gsrn));

        // Contract is included or not
        if (isIncluded)
        {
            syncInfo.Should().NotBeNull();
            syncInfo!.StartSyncDate.Should().Be(contractStart.ToDateTimeOffset());
        }
        else
        {
            syncInfo.Should().BeNull();
        }
    }

    [Fact]
    public async Task GivenEndedContractWithMissingMeasurements_WhenGettingSyncInfo_ContractIsIncluded()
    {
        // Given ended contract with missing measurements
        await using var dbContext = _dbContextFactoryMock.CreateDbContext();
        var gsrn = Any.Gsrn();
        var contractStart = UnixTimestamp.Now().AddHours(-20);
        var contractEnd = UnixTimestamp.Now().AddHours(-10);
        dbContext.Contracts.Add(Any.CertificateIssuingContract(gsrn, contractStart, contractEnd));
        dbContext.MeteringPointTimeSeriesSlidingWindows.Add(MeteringPointTimeSeriesSlidingWindow.Create(gsrn, contractEnd,
            [MeasurementInterval.Create(contractStart, contractStart.AddHours(1))]));
        await dbContext.SaveChangesAsync();

        // When getting sync info
        var syncInfo = (await _sut.GetSyncInfos()).FirstOrDefault(si => si.Gsrn.Equals(gsrn));

        // Contract is included
        syncInfo.Should().NotBeNull();
        syncInfo!.StartSyncDate.Should().Be(contractStart.ToDateTimeOffset());
    }

    [Fact]
    public async Task GivenEndedContractWithSlidingWindowWithNoMissingIntervals_WhenGettingSyncInfo_NotIncluded()
    {
        // Given ended contract with no missing measurements
        await using var dbContext = _dbContextFactoryMock.CreateDbContext();
        var gsrn = Any.Gsrn();
        var contractStart = UnixTimestamp.Now().AddHours(-20);
        var contractEnd = UnixTimestamp.Now().AddHours(-10);
        dbContext.Contracts.Add(Any.CertificateIssuingContract(gsrn, contractStart, contractEnd));
        dbContext.MeteringPointTimeSeriesSlidingWindows.Add(MeteringPointTimeSeriesSlidingWindow.Create(gsrn, contractEnd, []));
        await dbContext.SaveChangesAsync();

        // When getting sync info
        var syncInfo = (await _sut.GetSyncInfos()).FirstOrDefault(si => si.Gsrn.Equals(gsrn));

        // Contract is included
        syncInfo.Should().BeNull();
    }
}
