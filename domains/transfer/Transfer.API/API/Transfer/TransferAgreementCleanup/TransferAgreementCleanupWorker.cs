using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.TransferAgreementCleanup.Options;
using DataContext;
using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.Transfer.TransferAgreementCleanup;

public class TransferAgreementCleanupWorker(
    IDbContextFactory<TransferDbContext> contextFactory,
    ILogger<TransferAgreementCleanupWorker> logger,
    IOptions<TransferAgreementCleanupOptions> options)
    : BackgroundService
{
    private readonly TransferAgreementCleanupOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementCleanupWorker running at: {Time}", DateTimeOffset.UtcNow);

            try
            {
                await DeleteExpiredTransferAgreements(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError("Something went wrong with the TransferAgreementCleanupWorker: {Exception}", e);
            }

            await Sleep(stoppingToken);
        }
    }

    private async Task DeleteExpiredTransferAgreements(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var expiredTransferAgreements = context.TransferAgreements
            .Where(ta => ta.EndDate != null && ta.EndDate < DateTimeOffset.UtcNow);

        var expiredTaIds = expiredTransferAgreements.Select(ta => ta.Id).ToList();
        var historyEntries = context.TransferAgreementHistoryEntries
            .Where(h => expiredTaIds.Contains(h.TransferAgreementId));

        context.TransferAgreementHistoryEntries.RemoveRange(historyEntries);
        context.TransferAgreements.RemoveRange(expiredTransferAgreements);

        var senderLogEntries = expiredTransferAgreements.Select(transferAgreement => ActivityLogEntry.Create(Guid.Empty, ActivityLogEntry.ActorTypeEnum.System,
                       string.Empty, transferAgreement.SenderTin, transferAgreement.SenderName, string.Empty, string.Empty, ActivityLogEntry.EntityTypeEnum.TransferAgreement,
                                  ActivityLogEntry.ActionTypeEnum.Expired, transferAgreement.Id.ToString()));
        var receiverLogEntries = expiredTransferAgreements.Select(transferAgreement => ActivityLogEntry.Create(Guid.Empty, ActivityLogEntry.ActorTypeEnum.System,
            string.Empty, transferAgreement.ReceiverTin, string.Empty, string.Empty, string.Empty, ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            ActivityLogEntry.ActionTypeEnum.Expired, transferAgreement.Id.ToString()));
        await context.ActivityLogs.AddRangeAsync(senderLogEntries, cancellationToken);
        await context.ActivityLogs.AddRangeAsync(receiverLogEntries, cancellationToken);

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
