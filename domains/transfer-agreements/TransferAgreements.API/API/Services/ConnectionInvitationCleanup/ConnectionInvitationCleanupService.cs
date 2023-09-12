using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using Microsoft.Extensions.Logging;

namespace API.Services.ConnectionInvitationCleanup;

public class ConnectionInvitationCleanupService : IConnectionInvitationCleanupService
{
    private readonly ILogger<ConnectionInvitationCleanupService> logger;
    private readonly IConnectionInvitationRepository connectionInvitationRepository;

    public ConnectionInvitationCleanupService(
        ILogger<ConnectionInvitationCleanupService> logger,
        IConnectionInvitationRepository connectionInvitationRepository
    )
    {
        this.logger = logger;
        this.connectionInvitationRepository = connectionInvitationRepository;
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
        var minutesToNextHour = 60 - DateTimeOffset.UtcNow.Minute;
        logger.LogInformation("Sleeping until next full hour {MinutesToNextHour}", minutesToNextHour);
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), cancellationToken);
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation("Sleep was cancelled");
        }
    }
}
