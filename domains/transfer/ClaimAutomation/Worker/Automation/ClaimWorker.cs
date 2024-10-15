using System;
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

        await claimService.Run(stoppingToken);
    }
}
