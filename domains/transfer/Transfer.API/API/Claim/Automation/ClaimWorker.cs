using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace API.Claim.Automation;

public class ClaimWorker : BackgroundService
{
    private readonly IServiceProvider serviceProvider;

    public ClaimWorker(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var claimService = scope.ServiceProvider.GetRequiredService<IClaimService>();

        await claimService.Run(stoppingToken);
    }
}
