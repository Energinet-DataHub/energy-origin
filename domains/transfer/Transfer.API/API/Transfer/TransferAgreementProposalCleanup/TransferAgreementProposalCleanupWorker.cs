using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace API.Transfer.TransferAgreementProposalCleanup;

public class TransferAgreementProposalCleanupWorker(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<ITransferAgreementProposalCleanupService>();

        await cleanupService.Run(stoppingToken);
    }
}
