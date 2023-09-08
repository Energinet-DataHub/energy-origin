using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Services.ConnectionInvitationCleanup;

public class ConnectionInvitationCleanupWorker : BackgroundService
{
    private readonly ILogger<ConnectionInvitationCleanupWorker> logger;
    private readonly IServiceProvider serviceProvider;

    public ConnectionInvitationCleanupWorker(
        ILogger<ConnectionInvitationCleanupWorker> logger,
        IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var invitationCleanupService = scope.ServiceProvider.GetRequiredService<IConnectionInvitationCleanupService>();

        await invitationCleanupService.Run(stoppingToken);
    }
}
