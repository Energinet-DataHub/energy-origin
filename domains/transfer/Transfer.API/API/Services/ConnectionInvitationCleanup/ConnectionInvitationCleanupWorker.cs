using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace API.Services.ConnectionInvitationCleanup;

public class ConnectionInvitationCleanupWorker : BackgroundService
{
    private readonly IServiceProvider serviceProvider;

    public ConnectionInvitationCleanupWorker(
        IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var invitationCleanupService = scope.ServiceProvider.GetRequiredService<IConnectionInvitationCleanupService>();

        await invitationCleanupService.Run(stoppingToken);
    }
}
