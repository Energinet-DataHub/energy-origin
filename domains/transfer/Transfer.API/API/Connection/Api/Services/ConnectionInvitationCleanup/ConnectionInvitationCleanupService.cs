using System;
using System.Threading;
using System.Threading.Tasks;
using API.Connection.Api.Options;
using API.Connection.Api.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.Connection.Api.Services.ConnectionInvitationCleanup;

public class ConnectionInvitationCleanupService : IConnectionInvitationCleanupService
{
    private readonly ILogger<ConnectionInvitationCleanupService> logger;
    private readonly IConnectionInvitationRepository connectionInvitationRepository;
    private readonly ConnectionInvitationCleanupServiceOptions options;

    public ConnectionInvitationCleanupService(
        ILogger<ConnectionInvitationCleanupService> logger,
        IConnectionInvitationRepository connectionInvitationRepository,
        IOptions<ConnectionInvitationCleanupServiceOptions> options)
    {
        this.logger = logger;
        this.connectionInvitationRepository = connectionInvitationRepository;
        this.options = options.Value;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("InvitationCleanupService running at: {Time}", DateTimeOffset.UtcNow);

            try
            {
                await connectionInvitationRepository.DeleteOldConnectionInvitations(DateTimeOffset.UtcNow.AddDays(-14));
            }
            catch (Exception e)
            {
                logger.LogWarning("Something went wrong with the InvitationCleanupService: {Exception}", e);
            }

            await SleepToNearestHour(stoppingToken);
        }
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        logger.LogInformation("Sleep for {SleepTime}", options.SleepTime);
        try
        {
            await Task.Delay(options.SleepTime, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation("Sleep was cancelled");
        }
    }
}
