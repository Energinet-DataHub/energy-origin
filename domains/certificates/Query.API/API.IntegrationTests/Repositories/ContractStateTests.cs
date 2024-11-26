using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.IntegrationTests.Mocks;
using API.MeasurementsSyncer.Persistence;
using API.UnitTests;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace API.IntegrationTests.Repositories;

[Collection(IntegrationTestCollection.CollectionName)]
public class ContractStateIntegrationTests : IAsyncLifetime
{
    private readonly DbContextFactoryMock _dbContextFactoryMock;
    private readonly IOptions<MeasurementsSyncOptions> _optionsMock;

    public ContractStateIntegrationTests()
    {
        _dbContextFactoryMock = new DbContextFactoryMock();
        _optionsMock = Substitute.For<IOptions<MeasurementsSyncOptions>>();
        _optionsMock.Value.Returns(new MeasurementsSyncOptions { MinimumAgeThresholdHours = 50 });
    }

    private CertificateIssuingContract CreateContract(
        int contractNumber,
        Gsrn gsrn,
        DateTimeOffset startDate,
        DateTimeOffset? endDate = null,
        string gridArea = "DK1",
        MeteringPointType meteringPointType = MeteringPointType.Production,
        string meteringPointOwner = "SomeOwner",
        Guid? recipientId = null,
        Technology? technology = null)
    {
        return CertificateIssuingContract.Create(
            contractNumber,
            gsrn,
            gridArea,
            meteringPointType,
            meteringPointOwner,
            startDate,
            endDate,
            recipientId ?? Guid.NewGuid(),
            technology ?? Any.Technology());
    }

    [Fact]
    public async Task GetSyncInfos_WithNoContractsInDatabase_ReturnsEmptyList()
    {
        await using var dbContext = _dbContextFactoryMock.CreateDbContext();
        var contractState = new ContractState(_dbContextFactoryMock, Substitute.For<ILogger<ContractState>>(), _optionsMock);

        var syncInfos = await contractState.GetSyncInfos(CancellationToken.None);

        syncInfos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSyncInfos_WithActiveContracts_ReturnsOnlyContractsWhereStartDateIsBeforeMinimumAgeThreshold()
    {
        var contractActivationDate = DateTimeOffset.UtcNow;
        _optionsMock.Value.MinimumAgeThresholdHours = 100;
        var minimumAgeThreshold = UnixTimestamp.Create(contractActivationDate.AddHours(-_optionsMock.Value.MinimumAgeThresholdHours)).ToDateTimeOffset();

        await using (var dbContext = _dbContextFactoryMock.CreateDbContext())
        {
            dbContext.Contracts.Add(CreateContract(1, Any.Gsrn(), minimumAgeThreshold.AddHours(-200), minimumAgeThreshold.AddHours(-150)));
            dbContext.Contracts.Add(CreateContract(2, Any.Gsrn(), minimumAgeThreshold.AddHours(-200), contractActivationDate.AddHours(+9001))); // over NEIN thousaaaaand
            dbContext.Contracts.Add(CreateContract(3, Any.Gsrn(), contractActivationDate, null));
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = _dbContextFactoryMock.CreateDbContext();
        var contractState = new ContractState(_dbContextFactoryMock, Substitute.For<ILogger<ContractState>>(), _optionsMock);

        var syncInfos = await contractState.GetSyncInfos(CancellationToken.None);

        syncInfos.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSyncInfos_WithEndDatesWithinThreshold_ReturnsContracts()
    {
        var contractActivationDate = DateTimeOffset.UtcNow;
        _optionsMock.Value.MinimumAgeThresholdHours = 100;
        var minimumAgeThreshold = UnixTimestamp.Create(contractActivationDate.AddHours(-_optionsMock.Value.MinimumAgeThresholdHours)).ToDateTimeOffset();

        await using (var dbContext = _dbContextFactoryMock.CreateDbContext())
        {
            dbContext.Contracts.Add(CreateContract(1, Any.Gsrn(), minimumAgeThreshold.AddHours(-200), minimumAgeThreshold.AddHours(-150)));
            dbContext.Contracts.Add(CreateContract(2, Any.Gsrn(), contractActivationDate, null));
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = _dbContextFactoryMock.CreateDbContext();
        var contractState = new ContractState(_dbContextFactoryMock, Substitute.For<ILogger<ContractState>>(), _optionsMock);

        var syncInfos = await contractState.GetSyncInfos(CancellationToken.None);

        syncInfos.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSyncInfos_WhenSlidingWindowIsEmptyAndEndDateIsAfterThreshold_IncludesContract()
    {
        var gsrn = Any.Gsrn();
        var currentTime = UnixTimestamp.Now();

        await using (var dbContext = _dbContextFactoryMock.CreateDbContext())
        {
            dbContext.Contracts.Add(CreateContract(1, gsrn, currentTime.AddHours(-200).ToDateTimeOffset(), currentTime.AddHours(-10).ToDateTimeOffset()));
            var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, currentTime.AddHours(-150));
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = _dbContextFactoryMock.CreateDbContext();
        var contractState = new ContractState(_dbContextFactoryMock, Substitute.For<ILogger<ContractState>>(), _optionsMock);

        var syncInfos = await contractState.GetSyncInfos(CancellationToken.None);

        syncInfos.Should().HaveCount(1);
        syncInfos.First().Gsrn.Value.Should().Be(gsrn.Value);
    }

    [Fact]
    public async Task GetSyncInfos_WhenSlidingWindowHasMissingIntervalsAndEndDateIsBeforeThreshold_IncludesContract()
    {
        var gsrn = Any.Gsrn();
        var currentTime = UnixTimestamp.Now();

        _optionsMock.Value.MinimumAgeThresholdHours = 100;

        await using (var dbContext = _dbContextFactoryMock.CreateDbContext())
        {
            dbContext.Contracts.Add(CreateContract(
                1,
                gsrn,
                currentTime.AddHours(-200).ToDateTimeOffset(),
                currentTime.AddHours(-150).ToDateTimeOffset()));

            var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(
                gsrn,
                currentTime.AddHours(-150),
                new List<MeasurementInterval>
                {
                    MeasurementInterval.Create(
                        currentTime.AddHours(-200),
                        currentTime.AddHours(-100))
                });
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);

            await dbContext.SaveChangesAsync();
        }

        var contractState = new ContractState(_dbContextFactoryMock, Substitute.For<ILogger<ContractState>>(), _optionsMock);

        var syncInfos = await contractState.GetSyncInfos(CancellationToken.None);

        syncInfos.Should().HaveCount(1);
        syncInfos.First().Gsrn.Value.Should().Be(gsrn.Value);
    }

    [Fact]
    public async Task GetSyncInfos_WhenSlidingWindowDoesNotExistAndEndDateIsBeforeThreshold_IncludesContract()
    {
        var gsrn = Any.Gsrn();
        var currentTime = UnixTimestamp.Now();

        _optionsMock.Value.MinimumAgeThresholdHours = 100;

        await using (var dbContext = _dbContextFactoryMock.CreateDbContext())
        {
            dbContext.Contracts.Add(CreateContract(
                1,
                gsrn,
                currentTime.AddHours(-200).ToDateTimeOffset(),
                currentTime.AddHours(-150).ToDateTimeOffset()));

            await dbContext.SaveChangesAsync();
        }

        var contractState = new ContractState(_dbContextFactoryMock, Substitute.For<ILogger<ContractState>>(), _optionsMock);

        var syncInfos = await contractState.GetSyncInfos(CancellationToken.None);

        syncInfos.Should().HaveCount(1);
        syncInfos.First().Gsrn.Value.Should().Be(gsrn.Value);
    }

    [Fact]
    public async Task GetSyncInfos_WhenSlidingWindowIsEmptyAndEndDateIsNull_IncludesContract()
    {
        var gsrn = Any.Gsrn();
        var currentTime = UnixTimestamp.Now();

        _optionsMock.Value.MinimumAgeThresholdHours = 100;

        await using (var dbContext = _dbContextFactoryMock.CreateDbContext())
        {
            dbContext.Contracts.Add(CreateContract(
                1,
                gsrn,
                currentTime.AddHours(-200).ToDateTimeOffset(),
                null));

            var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, currentTime.AddHours(-150));
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);

            await dbContext.SaveChangesAsync();
        }

        var contractState = new ContractState(_dbContextFactoryMock, Substitute.For<ILogger<ContractState>>(), _optionsMock);

        var syncInfos = await contractState.GetSyncInfos(CancellationToken.None);

        syncInfos.Should().HaveCount(1);
        syncInfos.First().Gsrn.Value.Should().Be(gsrn.Value);
    }

    [Fact]
    public async Task GetSyncInfos_WhenSlidingWindowIsNotEmptyAndEndDateIsAfterThreshold_IncludesContract()
    {
        var gsrn = Any.Gsrn();
        var currentTime = UnixTimestamp.Now();

        await using (var dbContext = _dbContextFactoryMock.CreateDbContext())
        {
            dbContext.Contracts.Add(CreateContract(1, gsrn, currentTime.AddHours(-200).ToDateTimeOffset(), currentTime.AddHours(-10).ToDateTimeOffset()));
            var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, currentTime.AddHours(-150), new List<MeasurementInterval> { MeasurementInterval.Create(currentTime.AddHours(-200), currentTime.AddHours(-100)) });
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = _dbContextFactoryMock.CreateDbContext();
        var contractState = new ContractState(_dbContextFactoryMock, Substitute.For<ILogger<ContractState>>(), _optionsMock);

        var syncInfos = await contractState.GetSyncInfos(CancellationToken.None);

        syncInfos.Should().HaveCount(1);
        syncInfos.First().Gsrn.Value.Should().Be(gsrn.Value);
    }

    [Fact]
    public async Task GetSyncInfos_WhenSlidingWindowIsEmptyAndEndDateIsBeforeThreshold_ExcludesContract()
    {
        var gsrn = Any.Gsrn();
        var currentTime = UnixTimestamp.Now();

        await using (var dbContext = _dbContextFactoryMock.CreateDbContext())
        {
            dbContext.Contracts.Add(CreateContract(1, gsrn, currentTime.AddHours(-200).ToDateTimeOffset(), currentTime.AddHours(-200).ToDateTimeOffset()));
            var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(gsrn, currentTime.AddHours(-150));
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);
            await dbContext.SaveChangesAsync();
        }

        await using var newDbContext = _dbContextFactoryMock.CreateDbContext();
        var contractState = new ContractState(_dbContextFactoryMock, Substitute.For<ILogger<ContractState>>(), _optionsMock);

        var syncInfos = await contractState.GetSyncInfos(CancellationToken.None);

        syncInfos.Should().BeEmpty();
    }

    public Task InitializeAsync() => _dbContextFactoryMock.InitializeAsync();
    public Task DisposeAsync() => _dbContextFactoryMock.DisposeAsync();
}
