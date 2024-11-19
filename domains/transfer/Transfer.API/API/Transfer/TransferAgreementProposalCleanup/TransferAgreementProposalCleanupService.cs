using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.TransferAgreementProposalCleanup.Options;
using DataContext;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.TransferAgreementProposalCleanup;

public interface ITransferAgreementProposalCleanupService
{
    Task Run(CancellationToken stoppingToken);
}

public class TransferAgreementProposalCleanupService(
    ILogger<TransferAgreementProposalCleanupService> logger,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IOptions<TransferAgreementProposalCleanupServiceOptions> options)
    : ITransferAgreementProposalCleanupService
{
    private readonly TransferAgreementProposalCleanupServiceOptions options = options.Value;

    public async Task Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementProposalCleanupService running at: {Time}", DateTimeOffset.UtcNow);

            try
            {
                await using var dbContext = await contextFactory.CreateDbContextAsync(stoppingToken);
                if (await IsWaitingForMigrations(dbContext, stoppingToken))
                {
                    logger.LogInformation("Waiting for EF migrations");
                }
                else
                {
                    await DeleteOldTransferAgreementProposals(DateTimeOffset.UtcNow.AddDays(-14), stoppingToken);
                }
            }
            catch (Exception e)
            {
                logger.LogError("Something went wrong with the TransferAgreementProposalCleanupService: {Exception}", e);
            }

            await Sleep(stoppingToken);
        }
    }

    private async Task DeleteOldTransferAgreementProposals(DateTimeOffset olderThan, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var olderThanTimestamp = UnixTimestamp.Create(olderThan);
        var oldProposals = context.TransferAgreementProposals
            .Where(i => i.CreatedAt < olderThanTimestamp);

        context.TransferAgreementProposals.RemoveRange(oldProposals);

        var activityLogEntries = oldProposals.Select(proposal => ActivityLogEntry.Create(Guid.Empty, ActivityLogEntry.ActorTypeEnum.System,
            string.Empty, proposal.SenderCompanyTin.Value, proposal.SenderCompanyName.Value, String.Empty, String.Empty, ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal,
            ActivityLogEntry.ActionTypeEnum.Expired, proposal.Id.ToString()));
        await context.ActivityLogs.AddRangeAsync(activityLogEntries, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task Sleep(CancellationToken cancellationToken)
    {
        logger.LogInformation("Sleep for {SleepTime}", options.SleepTime);
        try
        {
            await Task.Delay(options.SleepTime, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation("Sleep was cancelled");
        }
    }

    private async Task<bool> IsWaitingForMigrations(DbContext dbContext, CancellationToken cancellationToken)
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        return pendingMigrations is null || pendingMigrations.Any();
    }
}
