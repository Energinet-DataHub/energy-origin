using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ClaimAutomation.Api.Repositories;
using ClaimAutomation.Worker.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProjectOriginClients;
using ProjectOriginClients.Models;

namespace ClaimAutomation.Worker.Automation;

public class ClaimService(
    ILogger<ClaimService> logger,
    IClaimAutomationRepository claimAutomationRepository,
    IProjectOriginWalletClient walletClient,
    IShuffler shuffle,
    IClaimAutomationMetrics metrics,
    AutomationCache cache)
    : IClaimService
{
    public int BatchSize { get; init; } = 5000;

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
                logger.LogInformation("Number of ClaimAutomationArguments for current run: {claimAutomationArguments}", claimAutomationArguments.Count);
                foreach (var subjectId in claimAutomationArguments.Select(x => x.SubjectId).Distinct())
                {
                    var hasMoreCertificates = true;
                    var certificates = new List<GranularCertificate>();
                    while (hasMoreCertificates)
                    {
                        var response = await walletClient.GetGranularCertificates(subjectId, stoppingToken, limit: BatchSize, skip: certificates.Count);

                        if (response == null)
                            throw new ClaimCertificatesException($"Something went wrong when getting certificates from the wallet for {subjectId}. Response is null.");

                        certificates.AddRange(response.Result);
                        if (certificates.Count >= response.Metadata.Total)
                        {
                            hasMoreCertificates = false;
                        }
                    }

                    certificates = certificates.OrderBy<GranularCertificate, int>(x => shuffle.Next()).ToList();
                    var certificatesGrouped = certificates.GroupBy(x => new { x.GridArea, x.Start, x.End });

                    foreach (var certGrp in certificatesGrouped)
                    {
                        var productionCerts = certGrp.Where(x => x.CertificateType == CertificateType.Production).ToList();
                        var consumptionCerts = certGrp.Where(x => x.CertificateType == CertificateType.Consumption).ToList();
                        logger.LogInformation("Claiming {productionCerts} production certs and {consumptionCerts} consumption certs for {subjectId}", productionCerts.Count, consumptionCerts.Count, subjectId);
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

            await walletClient.ClaimCertificates(subjectId, consumptionCert, productionCert, quantity);
            SetClaimAttempt(consumptionCert.FederatedStreamId.StreamId.ToString());
            SetClaimAttempt(productionCert.FederatedStreamId.StreamId.ToString());
            metrics.AddClaim();
            logger.LogInformation("Claimed {quantity} from production cert with id {productionCertId} and consumption cert with id {consumptionCertId}", quantity, productionCert.FederatedStreamId.StreamId, consumptionCert.FederatedStreamId.StreamId);

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
