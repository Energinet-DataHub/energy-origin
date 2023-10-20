using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace API.Claiming.Automation;

public class ClaimService : IClaimService
{
    private readonly ILogger<ClaimService> logger;

    public ClaimService(ILogger<ClaimService> logger)
    {
        this.logger = logger;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
            }
            catch (Exception e)
            {
                logger.LogWarning("Something went wrong with the ClaimService: {exception}", e);
            }

            await SleepAnHour(stoppingToken);
        }
    }

    private async Task SleepAnHour(CancellationToken cancellationToken)
    {
        logger.LogInformation("Sleeping for an hour.");
        await Task.Delay(TimeSpan.FromMinutes(60), cancellationToken);
    }
}
