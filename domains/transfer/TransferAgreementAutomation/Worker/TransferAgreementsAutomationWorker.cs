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

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementsAutomationWorker running at: {time}", DateTimeOffset.Now);
            metrics.ResetCertificatesTransferred();

            using var scope = provider.CreateScope();
            var projectOriginWalletService = scope.ServiceProvider.GetRequiredService<IProjectOriginWalletService>();

            try
            {
                var transferAgreements = await GetAllTransferAgreements(stoppingToken);
                metrics.SetNumberOfTransferAgreements(transferAgreements.Count);

                foreach (var transferAgreement in transferAgreements)
                {
                    await projectOriginWalletService.TransferCertificates(transferAgreement);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Something went wrong with the TransferAgreementsAutomationWorker: {exception}", ex);
            }

            await SleepToNearestHour(stoppingToken);
        }
    }

    private async Task<List<TransferAgreement>> GetAllTransferAgreements(CancellationToken stoppingToken)
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
