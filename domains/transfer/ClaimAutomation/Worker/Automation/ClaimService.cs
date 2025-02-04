using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ClaimAutomation.Api.Repositories;
using ClaimAutomation.Worker.Metrics;
using ClaimAutomation.Worker.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;

namespace ClaimAutomation.Worker.Automation;

public class ClaimService(
    ILogger<ClaimService> logger,
    IClaimAutomationRepository claimAutomationRepository,
    IWalletClient walletClient,
    IShuffler shuffle,
    IClaimAutomationMetrics metrics,
    AutomationCache cache,
    IOptions<ClaimAutomationOptions> options)
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
                logger.LogInformation("Number of ClaimAutomationArguments for current run: {claimAutomationArguments}",
                    claimAutomationArguments.Count);
                foreach (var subjectId in claimAutomationArguments.Select(x => x.SubjectId).Distinct())
                {
                    var certificates = await FetchAllCertificatesFromWallet(subjectId, stoppingToken);

                    certificates = certificates.OrderBy<GranularCertificate, int>(x => shuffle.Next()).ToList();
                    var certificatesGrouped = certificates.GroupBy(x => new { x.GridArea, x.Start, x.End });

                    foreach (var certGrp in certificatesGrouped)
                    {
                        var productionCerts = certGrp.Where(x => x.CertificateType == CertificateType.Production).ToList();
                        var consumptionCerts = certGrp.Where(x => x.CertificateType == CertificateType.Consumption).ToList();
                        logger.LogInformation("Claiming {productionCerts} production certs and {consumptionCerts} consumption certs for {subjectId}",
                            productionCerts.Count, consumptionCerts.Count, subjectId);
                        await Claim(subjectId, consumptionCerts, productionCerts);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError("Something went wrong with the ClaimService: {exception}", e);
            }

            await Sleep(stoppingToken);
        }
    }

    private async Task<List<GranularCertificate>> FetchAllCertificatesFromWallet(Guid subjectId, CancellationToken stoppingToken)
    {
        var hasMoreCertificates = true;
        var certificates = new List<GranularCertificate>();
        while (hasMoreCertificates)
        {
            var response = await walletClient.GetGranularCertificates(subjectId, stoppingToken,
                limit: options.Value.CertificateFetchBachSize, skip: certificates.Count);

            if (response == null)
            {
                throw new ClaimCertificatesException(
                    $"Something went wrong when getting certificates from the wallet for {subjectId}. Response is null.");
            }

            certificates.AddRange(response.Result);
            if (certificates.Count >= response.Metadata.Total)
            {
                hasMoreCertificates = false;
            }
        }

        return certificates;
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

            productionCert.Quantity -= quantity;
            consumptionCert.Quantity -= quantity;

            if (productionCert.Quantity == 0)
            {
                metrics.AddCertificateClaimedThisRun();
            }

            if (consumptionCert.Quantity == 0)
            {
                metrics.AddCertificateClaimedThisRun();
            }
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

    private async Task Sleep(CancellationToken cancellationToken)
    {
        if (options.Value.ScheduleInterval == ScheduleInterval.EveryHourHalfPast)
        {
            var minutesToNextHalfHour = TimeSpanHelper.GetMinutesToNextHalfHour(DateTimeOffset.Now.Minute);
            logger.LogInformation("Sleeping until next half past {minutesToNextHalfHour}", minutesToNextHalfHour);
            await Task.Delay(TimeSpan.FromMinutes(minutesToNextHalfHour), cancellationToken);
        }
        else
        {
            logger.LogInformation("Sleeping 5 seconds");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }
}
