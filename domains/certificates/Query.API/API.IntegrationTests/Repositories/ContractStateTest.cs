using System.Linq;
using System.Threading.Tasks;
using API.Configurations;
using API.IntegrationTests.Mocks;
using API.MeasurementsSyncer.Persistence;
using API.UnitTests;
using DataContext.Models;
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
