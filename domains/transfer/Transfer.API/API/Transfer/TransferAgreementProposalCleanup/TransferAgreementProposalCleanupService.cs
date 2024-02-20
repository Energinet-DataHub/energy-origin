using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.TransferAgreementProposalCleanup.Options;
using DataContext;
using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.TransferAgreementProposalCleanup;

public interface ITransferAgreementProposalCleanupService
{
    Task Run(CancellationToken stoppingToken);
}

public class TransferAgreementProposalCleanupService : ITransferAgreementProposalCleanupService
{
    private readonly ILogger<TransferAgreementProposalCleanupService> logger;
    private readonly IDbContextFactory<ApplicationDbContext> contextFactory;
    private readonly TransferAgreementProposalCleanupServiceOptions options;

    public TransferAgreementProposalCleanupService(
        ILogger<TransferAgreementProposalCleanupService> logger,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IOptions<TransferAgreementProposalCleanupServiceOptions> options)
    {
        this.logger = logger;
        this.contextFactory = contextFactory;
        this.options = options.Value;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementProposalCleanupService running at: {Time}", DateTimeOffset.UtcNow);

            try
            {
                await DeleteOldTransferAgreementProposals(DateTimeOffset.UtcNow.AddDays(-14), stoppingToken);
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

        var oldProposals = context.TransferAgreementProposals
            .Where(i => i.CreatedAt < olderThan);

        context.TransferAgreementProposals.RemoveRange(oldProposals);

        var activityLogEntries = oldProposals.Select(proposal => ActivityLogEntry.Create(Guid.Empty, ActivityLogEntry.ActorTypeEnum.System,
            string.Empty, string.Empty, string.Empty, ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal,
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
}
