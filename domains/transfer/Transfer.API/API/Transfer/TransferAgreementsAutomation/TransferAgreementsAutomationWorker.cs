using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.TransferAgreementsAutomation.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Transfer.TransferAgreementsAutomation;

public class TransferAgreementsAutomationWorker : BackgroundService
{
    private readonly ILogger<TransferAgreementsAutomationWorker> logger;
    private readonly IServiceProvider serviceProvider;

    public TransferAgreementsAutomationWorker(
        ILogger<TransferAgreementsAutomationWorker> logger,
        IServiceProvider serviceProvider
    )
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var transferAgreementsAutomationService = scope.ServiceProvider.GetRequiredService<ITransferAgreementsAutomationService>();

        await transferAgreementsAutomationService.Run(stoppingToken);
    }
}
