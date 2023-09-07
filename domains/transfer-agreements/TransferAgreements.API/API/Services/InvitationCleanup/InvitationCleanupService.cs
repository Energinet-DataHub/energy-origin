using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using Microsoft.Extensions.Logging;

namespace API.Services.InvitationCleanup;

public class InvitationCleanupService : IInvitationCleanupService
{
    private readonly ILogger<InvitationCleanupService> logger;
    private readonly IInvitationRepository invitationRepository;

    public InvitationCleanupService(
        ILogger<InvitationCleanupService> logger,
        IInvitationRepository invitationRepository
    )
    {
        this.logger = logger;
        this.invitationRepository = invitationRepository;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("InvitationCleanupService running at: {Time}", DateTimeOffset.Now);

            try
            {
                await invitationRepository.DeleteOldInvitations(TimeSpan.FromDays(14));
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
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
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
