using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Services.InvitationCleanup;

public class InvitationCleanupWorker : BackgroundService
{
    private readonly ILogger<InvitationCleanupWorker> logger;
    private readonly IServiceProvider serviceProvider;

    public InvitationCleanupWorker(
        ILogger<InvitationCleanupWorker> logger,
        IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var invitationCleanupService = scope.ServiceProvider.GetRequiredService<IInvitationCleanupService>();

        await invitationCleanupService.Run(stoppingToken);
    }
}
