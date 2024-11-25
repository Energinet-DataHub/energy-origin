using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using DataContext;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.IssuingContractCleanup;

public class IssuingContractCleanupService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<IssuingContractCleanupService> logger;
    private readonly MeasurementsSyncOptions options;

    public IssuingContractCleanupService(ApplicationDbContext dbContext, ILogger<IssuingContractCleanupService> logger, IOptions<MeasurementsSyncOptions> options)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        this.options = options.Value;
    }

    public async Task RunCleanupJob(CancellationToken stoppingToken)
    {
        if (await IsWaitingForMigrations(stoppingToken))
        {
            logger.LogInformation("Waiting for EF migrations");
        }
        else
        {
            await DeleteExpiredIssuingContracts(stoppingToken);
        }
    }

    private async Task DeleteExpiredIssuingContracts(CancellationToken cancellationToken)
    {
        var expiredIssuingContracts = dbContext.Contracts
            .Where(c => c.EndDate != null && c.EndDate < UnixTimestamp.Now().Add(-TimeSpan.FromHours(options.MinimumAgeThresholdHours)).ToDateTimeOffset());

        dbContext.Contracts.RemoveRange(expiredIssuingContracts);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsWaitingForMigrations(CancellationToken cancellationToken)
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        return pendingMigrations is null || pendingMigrations.Any();
    }
}
