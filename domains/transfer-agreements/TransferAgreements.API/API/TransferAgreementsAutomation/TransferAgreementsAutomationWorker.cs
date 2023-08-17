using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.TransferAgreementsAutomation;

public class TransferAgreementsAutomationWorker : BackgroundService
{
    private readonly ILogger<TransferAgreementsAutomationWorker> logger;
    private readonly TransferAgreementAutomationService service;

    public TransferAgreementsAutomationWorker(
        ILogger<TransferAgreementsAutomationWorker> logger,
        TransferAgreementAutomationService service
    )
    {
        this.logger = logger;
        this.service = service;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            service.Run();
            await SleepToNearestHour(stoppingToken);
        }
    }

    private async Task SleepToNearestHour(CancellationToken stoppingToken)
    {
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        logger.LogInformation("Sleeping until next full hour {minutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), stoppingToken);
    }
}
