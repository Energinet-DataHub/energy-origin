using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Options;
using TransferAgreementAutomation.Worker.Service;

namespace TransferAgreementAutomation.Worker;

public class TransferAgreementsAutomationWorker(
    ILogger<TransferAgreementsAutomationWorker> logger,
    ITransferAgreementAutomationMetrics metrics,
    IServiceProvider provider,
    IOptions<TransferAgreementAutomationOptions> options,
    IDbContextFactory<ApplicationDbContext> contextFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("TransferAgreementsAutomationWorker is disabled!");
            return;
        }

        var done = false;
        while (!done)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                logger.LogInformation("TransferAgreementsAutomationWorker running at: {time}", DateTimeOffset.Now);
                metrics.ResetCertificatesTransferred();
                var transferAgreements = await GetTransferAgreements(stoppingToken);
                metrics.SetNumberOfTransferAgreements(transferAgreements.Count);

                transferAgreements.Sort(new TransferAgreementProcessOrderComparer());

                foreach (var transferAgreement in transferAgreements)
                {
                    using var scope = provider.CreateScope();
                    var transferEngine = scope.ServiceProvider.GetRequiredService<ITransferEngineCoordinator>();
                    await transferEngine.TransferCertificate(transferAgreement, stoppingToken);
                }
                await SleepToNearestHour(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("TransferAgreementsAutomationWorker has been cancelled");
                done = true;
            }
            catch (Exception ex)
            {
                logger.LogError("Something went wrong with the TransferAgreementsAutomationWorker: {exception}", ex);
            }

        }
    }

    private async Task<List<TransferAgreement>> GetTransferAgreements(CancellationToken stoppingToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(stoppingToken);
        return await dbContext.TransferAgreements.ToListAsync(stoppingToken);
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        logger.LogInformation("Sleeping until next full hour {minutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), cancellationToken);
    }
}
