using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Claiming.Api.Repositories;
using API.Shared.Services;
using Microsoft.Extensions.Logging;
using ProjectOrigin.WalletSystem.V1;

namespace API.Claiming.Automation;

public class ClaimService : IClaimService
{
    private readonly ILogger<ClaimService> logger;
    private readonly IClaimRepository claimRepository;
    private readonly IProjectOriginWalletService walletService;

    public ClaimService(ILogger<ClaimService> logger, IClaimRepository claimRepository, IProjectOriginWalletService walletService)
    {
        this.logger = logger;
        this.claimRepository = claimRepository;
        this.walletService = walletService;
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
                    var certificates = await walletService.GetGranularCertificates(subjectId);
                    var consumptionCertificates = certificates.Where(x => x.Type == GranularCertificateType.Consumption).ToList();
                    var productionCertificates = certificates.Where(x => x.Type == GranularCertificateType.Production).ToList();

                    foreach (var consumptionCertificate in consumptionCertificates)
                    {
                        var productionCertificate = productionCertificates.FirstOrDefault(x =>
                            x.Start == consumptionCertificate.Start && x.End == consumptionCertificate.End &&
                            x.GridArea == consumptionCertificate.GridArea);
                        if (productionCertificate == null) continue;

                        var quantity = productionCertificate.Quantity;

                        if (productionCertificate.Quantity > consumptionCertificate.Quantity)
                            quantity = consumptionCertificate.Quantity;

                        await walletService.ClaimCertificate(subjectId, consumptionCertificate, productionCertificate, quantity);

                        productionCertificate.Quantity -= quantity;
                        if (productionCertificate.Quantity <= 0)
                            productionCertificates.Remove(productionCertificate);
                    }
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
