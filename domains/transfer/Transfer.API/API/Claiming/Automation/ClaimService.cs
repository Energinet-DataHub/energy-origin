using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Claiming.Api.Repositories;
using API.Claiming.Automation.Services;
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
                foreach (var subjectId in claimSubjects.Select(x => x.SubjectId).Distinct())
                {
                    var certificates = await walletService.GetGranularCertificates(subjectId);

                    var certificatesGrouped = certificates.GroupBy(x => new { x.GridArea, x.Start, x.End });

                    foreach (var cert in certificatesGrouped)
                    {
                        var productionCerts = cert.Where(x => x.Type == GranularCertificateType.Production).ToList();
                        var consumptionCerts = cert.Where(x => x.Type == GranularCertificateType.Consumption).ToList();

                        await Claim(subjectId, consumptionCerts, productionCerts);
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

    private async Task Claim(Guid subjectId, List<GranularCertificate> consumptionCertificates, List<GranularCertificate> productionCertificates)
    {
        while (productionCertificates.Any(x => x.Quantity > 0) && consumptionCertificates.Any(x => x.Quantity > 0))
        {
            var productionCert = productionCertificates.FirstOrDefault(x => x.Quantity > 0);
            if (productionCert == null) continue;
            var consumptionCert = consumptionCertificates.FirstOrDefault(x => x.Quantity > 0);
            if (consumptionCert == null) continue;

            var quantity = Math.Min(productionCert.Quantity, consumptionCert.Quantity);

            await walletService.ClaimCertificate(subjectId, consumptionCert, productionCert, quantity);

            productionCert.Quantity -= quantity;
            consumptionCert.Quantity -= quantity;
        }
    }

    private async Task SleepAnHour(CancellationToken cancellationToken)
    {
        logger.LogInformation("Sleeping for an hour.");
        await Task.Delay(TimeSpan.FromMinutes(60), cancellationToken);
    }
}
