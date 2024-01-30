using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClaimAutomation.Worker.Api.Repositories;
using ClaimAutomation.Worker.Automation.Services;
using ClaimAutomation.Worker.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProjectOrigin.WalletSystem.V1;

namespace ClaimAutomation.Worker.Automation;

public class ClaimService(
    ILogger<ClaimService> logger,
    IClaimAutomationRepository claimAutomationRepository,
    IProjectOriginWalletService walletService,
    IShuffler shuffle,
    IClaimAutomationMetrics metrics,
    AutomationCache cache)
    : IClaimService
{
    public async Task Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("ClaimService running at: {time}", DateTimeOffset.Now);
            metrics.ResetCertificatesClaimed();
            metrics.ResetClaimErrors();
            metrics.ResetNumberOfClaims();
            try
            {
                var claimAutomationArguments = await claimAutomationRepository.GetClaimAutomationArguments();
                foreach (var subjectId in claimAutomationArguments.Select(x => x.SubjectId).Distinct())
                {
                    var certificates = await walletService.GetGranularCertificates(subjectId);

                    certificates = certificates.OrderBy<GranularCertificate, int>(x => shuffle.Next()).ToList();

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
                logger.LogError("Something went wrong with the ClaimService: {exception}", e);
            }

            await SleepAnHour(stoppingToken);
        }
    }

    private async Task Claim(Guid subjectId, List<GranularCertificate> consumptionCertificates, List<GranularCertificate> productionCertificates)
    {
        while (productionCertificates.Any(x => x.Quantity > 0) && consumptionCertificates.Any(x => x.Quantity > 0))
        {
            var productionCert = productionCertificates.First(x => x.Quantity > 0);
            var consumptionCert = consumptionCertificates.First(x => x.Quantity > 0);

            var quantity = Math.Min(productionCert.Quantity, consumptionCert.Quantity);

            await walletService.ClaimCertificates(subjectId, consumptionCert, productionCert, quantity);
            SetClaimAttempt(consumptionCert.FederatedId.StreamId.ToString());
            SetClaimAttempt(productionCert.FederatedId.StreamId.ToString());
            metrics.AddClaim();

            productionCert.Quantity -= quantity;
            consumptionCert.Quantity -= quantity;

            if (productionCert.Quantity == 0)
                metrics.AddCertificateClaimedThisRun();
            if (consumptionCert.Quantity == 0)
                metrics.AddCertificateClaimedThisRun();
        }
    }

    private void SetClaimAttempt(string certificateId)
    {
        var attempt = cache.Cache.Get(certificateId);
        if (attempt == null)
        {
            cache.Cache.Set(certificateId, 1, TimeSpan.FromHours(2));
        }
        else
        {
            metrics.AddClaimError();
        }
    }

    private async Task SleepAnHour(CancellationToken cancellationToken)
    {
        var minutesToNextHalfHour = TimeSpanHelper.GetMinutesToNextHalfHour(DateTimeOffset.Now.Minute);

        logger.LogInformation("Sleeping until next half past {minutesToNextHalfHour}", minutesToNextHalfHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHalfHour), cancellationToken);
    }
}
