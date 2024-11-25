using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.IssuingContractCleanup;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.IntegrationTests.IssuingContractCleanup;

[Collection(IntegrationTestCollection.CollectionName)]
public class IssuingContractCleanupTests
{
    private readonly PostgresContainer postgresContainer;

    public IssuingContractCleanupTests(IntegrationTestFixture integrationTestFixture)
    {
        postgresContainer = integrationTestFixture.PostgresContainer;
    }

    [Fact]
    public async Task IssuingContractCleanup_WithZeroMinimumAgeThreshold_ShouldDeleteContractsEndingBeforeNow()
    {
        // Arrange
        var dbContext = GetDbContext();
        dbContext.Contracts.RemoveRange(dbContext.Contracts);
        await dbContext.SaveChangesAsync();

        var options = Options.Create(new MeasurementsSyncOptions
        {
            MinimumAgeThresholdHours = 0
        });

        var logger = new NullLogger<IssuingContractCleanupService>();

        var service = new IssuingContractCleanupService(dbContext, logger, options);

        var expiredContract = new CertificateIssuingContract
        {
            Id = Guid.NewGuid(),
            GSRN = "5734567890123456",
            EndDate = DateTimeOffset.UtcNow.AddHours(-1),
            MeteringPointOwner = Guid.NewGuid().ToString(),
            ContractNumber = 0,
            Created = DateTimeOffset.UtcNow.AddHours(-2),
            GridArea = "DK1",
            MeteringPointType = MeteringPointType.Production,
            StartDate = DateTimeOffset.UtcNow.AddHours(-2),
            Technology = new Technology("SomeFuelCode", "SomeTechCode"),
            RecipientId = Guid.NewGuid()
        };

        var activeContract = new CertificateIssuingContract
        {
            Id = Guid.NewGuid(),
            GSRN = "5734567890123457",
            EndDate = DateTimeOffset.UtcNow.AddHours(1),
            MeteringPointOwner = Guid.NewGuid().ToString(),
            ContractNumber = 1,
            Created = DateTimeOffset.UtcNow.AddHours(-1),
            GridArea = "DK1",
            MeteringPointType = MeteringPointType.Production,
            StartDate = DateTimeOffset.UtcNow.AddHours(-1),
            Technology = new Technology("SomeFuelCode", "SomeTechCode"),
            RecipientId = Guid.NewGuid()
        };

        dbContext.Contracts.AddRange(expiredContract, activeContract);
        await dbContext.SaveChangesAsync();

        // Act
        await service.RunCleanupJob(CancellationToken.None);

        // Assert
        var remainingContracts = dbContext.Contracts.ToList();
        remainingContracts.Count.Should().Be(1);
        remainingContracts.Single().Id.Should().Be(activeContract.Id);
    }

    [Fact]
    public async Task IssuingContractCleanup_WithMinimumAgeThreshold_ShouldDeleteContractsOlderThanThreshold()
    {
        // Arrange
        var dbContext = GetDbContext();
        dbContext.Contracts.RemoveRange(dbContext.Contracts);
        await dbContext.SaveChangesAsync();

        var options = Options.Create(new MeasurementsSyncOptions
        {
            MinimumAgeThresholdHours = 2
        });

        var logger = new NullLogger<IssuingContractCleanupService>();

        var service = new IssuingContractCleanupService(dbContext, logger, options);

        var contractToDelete = new CertificateIssuingContract
        {
            Id = Guid.NewGuid(),
            GSRN = "5734567890123458",
            EndDate = DateTimeOffset.UtcNow.AddHours(-3),
            MeteringPointOwner = Guid.NewGuid().ToString(),
            ContractNumber = 2,
            Created = DateTimeOffset.UtcNow.AddHours(-4),
            GridArea = "DK1",
            MeteringPointType = MeteringPointType.Production,
            StartDate = DateTimeOffset.UtcNow.AddHours(-4),
            Technology = new Technology("SomeFuelCode", "SomeTechCode"),
            RecipientId = Guid.NewGuid()
        };

        var contractToKeep = new CertificateIssuingContract
        {
            Id = Guid.NewGuid(),
            GSRN = "5734567890123459",
            EndDate = DateTimeOffset.UtcNow.AddHours(-1),
            MeteringPointOwner = Guid.NewGuid().ToString(),
            ContractNumber = 3,
            Created = DateTimeOffset.UtcNow.AddHours(-2),
            GridArea = "DK1",
            MeteringPointType = MeteringPointType.Production,
            StartDate = DateTimeOffset.UtcNow.AddHours(-2),
            Technology = new Technology("SomeFuelCode", "SomeTechCode"),
            RecipientId = Guid.NewGuid()
        };

        var futureContract = new CertificateIssuingContract
        {
            Id = Guid.NewGuid(),
            GSRN = "5734567890123460",
            EndDate = DateTimeOffset.UtcNow.AddHours(2),
            MeteringPointOwner = Guid.NewGuid().ToString(),
            ContractNumber = 4,
            Created = DateTimeOffset.UtcNow.AddHours(-1),
            GridArea = "DK1",
            MeteringPointType = MeteringPointType.Production,
            StartDate = DateTimeOffset.UtcNow.AddHours(-1),
            Technology = new Technology("SomeFuelCode", "SomeTechCode"),
            RecipientId = Guid.NewGuid()
        };

        dbContext.Contracts.AddRange(contractToDelete, contractToKeep, futureContract);
        await dbContext.SaveChangesAsync();

        // Act
        await service.RunCleanupJob(CancellationToken.None);

        // Assert
        var remainingContracts = dbContext.Contracts.ToList();
        remainingContracts.Count.Should().Be(2);
        remainingContracts.Select(c => c.Id).Should().Contain(contractToKeep.Id);
        remainingContracts.Select(c => c.Id).Should().Contain(futureContract.Id);
        remainingContracts.Select(c => c.Id).Should().NotContain(contractToDelete.Id);
    }

    private ApplicationDbContext GetDbContext()
    {
        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgresContainer.ConnectionString)
            .Options;
        return new ApplicationDbContext(contextOptions);
    }
}
