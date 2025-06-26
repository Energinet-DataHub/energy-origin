using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        var done = false;
        while (!done)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                logger.LogInformation("ClaimService running at: {time}", DateTimeOffset.Now);
                metrics.ResetCertificatesClaimed();
                metrics.ResetClaimErrors();
                metrics.ResetNumberOfClaims();

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
                        var productionCertsTrial = certGrp
                            .Where(x => x is { CertificateType: CertificateType.Production, IsTrialCertificate: true })
                            .ToList();

                        var productionCertsNonTrial = certGrp
                            .Where(x => x is { CertificateType: CertificateType.Production, IsTrialCertificate: false })
                            .ToList();

                        var consumptionCertsTrial = certGrp
                            .Where(x => x is { CertificateType: CertificateType.Consumption, IsTrialCertificate: true })
                            .ToList();

                        var consumptionCertsNonTrial = certGrp
                            .Where(x => x is { CertificateType: CertificateType.Consumption, IsTrialCertificate: false })
                            .ToList();

                        await Claim(subjectId, consumptionCertsNonTrial, productionCertsNonTrial, stoppingToken);
                        await Claim(subjectId, consumptionCertsTrial, productionCertsTrial, stoppingToken);
                    }
                }
                await Sleep(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("ClaimService was cancelled");
                done = true;
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case HttpRequestException { StatusCode: HttpStatusCode.BadRequest }:
                        logger.LogWarning("Wallet returned bad request: {exception}", e);
                        break;
                    default:
                        logger.LogError("Something went wrong with the ClaimService: {exception}", e);
                        break;
                }
            }

        }
    }

    private async Task<List<GranularCertificate>> FetchAllCertificatesFromWallet(Guid subjectId, CancellationToken stoppingToken)
    {

        var subjectIdStr = subjectId.ToString();
        var maskedSubjectId = subjectIdStr[..^6] + new string('*', 6);

        var hasMoreCertificates = true;
        var certificates = new List<GranularCertificate>();

        while (hasMoreCertificates)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                var response = await walletClient.GetGranularCertificatesAsync(subjectId, stoppingToken,
                    limit: options.Value.CertificateFetchBachSize, skip: certificates.Count);

                if (response == null)
                {
                    throw new ClaimCertificatesException(
                        $"Something went wrong when getting certificates from the wallet for {maskedSubjectId}. Response is null.");
                }

                certificates.AddRange(response.Result);
                if (certificates.Count >= response.Metadata.Total)
                {
                    hasMoreCertificates = false;
                }
            }
            catch (Exception e)
            {
                hasMoreCertificates = false;
                logger.LogError(e, "Error fetching certificates for subject {MaskedSubjectId}", maskedSubjectId);
            }
        }

        return certificates;
    }

    private async Task Claim(Guid subjectId, List<GranularCertificate> consumptionCertificates, List<GranularCertificate> productionCertificates, CancellationToken cancellationToken)
    {
        while (productionCertificates.Any(x => x.Quantity > 0) && consumptionCertificates.Any(x => x.Quantity > 0))
        {

            var productionCert = productionCertificates.First(x => x.Quantity > 0);
            var consumptionCert = consumptionCertificates.First(x => x.Quantity > 0);

            var quantity = Math.Min(productionCert.Quantity, consumptionCert.Quantity);
            logger.LogInformation("Claiming Quanitity: {Quantity}. " +
                                  "Consumption Certificate: {ConsumptionCert} Consumption Quantity: {ConsumptionCertQuanitity}. " +
                                  "Production Certificate: {ProductionCert}. Production Quantity: {ProductionQuantity}" +
                                  "Organization: {SubjectId}",
                quantity, consumptionCert.FederatedStreamId.StreamId, consumptionCert.Quantity, productionCert.FederatedStreamId.StreamId, productionCert.Quantity, subjectId);
            cancellationToken.ThrowIfCancellationRequested();
            await walletClient.ClaimCertificatesAsync(subjectId, consumptionCert, productionCert, quantity, cancellationToken);

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
