using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Claiming.Api.Repositories;
using Microsoft.Extensions.Logging;

namespace API.Claiming.Automation;

public class ClaimService : IClaimService
{
    private readonly ILogger<ClaimService> logger;
    private readonly IClaimRepository claimRepository;

    public ClaimService(ILogger<ClaimService> logger, IClaimRepository claimRepository)
    {
        this.logger = logger;
        this.claimRepository = claimRepository;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("ClaimService running at: {time}", DateTimeOffset.Now);
            try
            {
                var claimSubjects = await claimRepository.GetClaimSubjects();
                foreach (var subjectId in claimSubjects.Select(x => x.SubjectId))
                {
                    //Get certs

                    //Claim
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Something went wrong with the ClaimService: {exception}", e);
            }

            await SleepAnHour(stoppingToken);
        }
    }

    private async Task SleepAnHour(CancellationToken cancellationToken)
    {
        logger.LogInformation("Sleeping for an hour.");
        await Task.Delay(TimeSpan.FromMinutes(60), cancellationToken);
    }
}
