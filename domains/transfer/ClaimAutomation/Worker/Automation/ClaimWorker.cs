using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClaimAutomation.Worker.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaimAutomation.Worker.Automation;

public class ClaimWorker : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<ClaimWorker> logger;
    private readonly ClaimAutomationOptions options;

    public ClaimWorker(IServiceProvider serviceProvider, ILogger<ClaimWorker> logger, IOptions<ClaimAutomationOptions> options)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("ClaimWorker is disabled!");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var claimService = scope.ServiceProvider.GetRequiredService<IClaimService>();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await claimService.Run(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("ClaimService was cancelled");
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case HttpRequestException { StatusCode: HttpStatusCode.BadRequest }:
                        logger.LogWarning("Wallet returned bad request: {exception}", e);
                        break;
                    default:
                        logger.LogError("Something went wrong with the ClaimService: {exception}", e);
                        break;
                }
            }
            await Sleep(stoppingToken);
        }
    }
    private async Task Sleep(CancellationToken cancellationToken)
    {
        if (options.ScheduleInterval == ScheduleInterval.EveryHourHalfPast)
        {
            var minutesToNextHalfHour = TimeSpanHelper.GetMinutesToNextHalfHour(DateTime.Now.Minute);
            logger.LogInformation("Sleeping until next half past {minutesToNextHalfHour}", minutesToNextHalfHour);
            await Task.Delay(TimeSpan.FromMinutes(minutesToNextHalfHour), cancellationToken);
        }
        else
        {
            logger.LogInformation("Sleeping 5 seconds");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }
}
