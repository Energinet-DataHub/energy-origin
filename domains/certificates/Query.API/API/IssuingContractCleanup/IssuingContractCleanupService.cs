using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.IssuingContractCleanup;

public class IssuingContractCleanupService(ApplicationDbContext dbContext, ILogger<IssuingContractCleanupService> logger)
{
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
            .Where(c => c.EndDate != null && c.EndDate < DateTimeOffset.UtcNow);

        dbContext.Contracts.RemoveRange(expiredIssuingContracts);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsWaitingForMigrations(CancellationToken cancellationToken)
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        return pendingMigrations is null || pendingMigrations.Any();
    }
}
