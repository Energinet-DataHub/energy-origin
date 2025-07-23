using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using Microsoft.Extensions.Logging;

namespace API.ReportGenerator.Infrastructure;

public interface IEnergyDataFetcher
{
    Task<(List<ConsumptionHour> totalHourConsumption, List<ConsumptionHour> averageHourConsumption, List<Claim> claims)> GetAsync(OrganizationId orgId, DateTimeOffset from, DateTimeOffset to, bool isTrial, CancellationToken ct = default);
}

public sealed class EnergyDataFetcher : IEnergyDataFetcher
{
    private readonly IConsumptionService _consumptionService;
    private readonly IWalletClient _walletClient;
    private readonly ILogger<EnergyDataFetcher> _logger;

    public EnergyDataFetcher(
        IConsumptionService consumptionService,
        IWalletClient walletClient,
        ILogger<EnergyDataFetcher> logger)
    {
        _consumptionService = consumptionService ?? throw new ArgumentNullException(nameof(consumptionService));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _logger = logger;
    }

    public async Task<(List<ConsumptionHour> totalHourConsumption, List<ConsumptionHour> averageHourConsumption, List<Claim> claims)> GetAsync(
        OrganizationId orgId,
        DateTimeOffset from,
        DateTimeOffset to,
        bool isTrial,
        CancellationToken ct = default)
    {
        if (orgId is null) throw new ArgumentNullException(nameof(orgId));

        var allConsumptionFetchedFromDatahub = _consumptionService.GetTotalAndAverageHourlyConsumption(orgId, from, to, ct);
        var allClamsFetchedFromWallet = _walletClient.GetClaimsAsync(orgId.Value, from, to, TimeMatch.All, ct);


        await Task.WhenAll(allConsumptionFetchedFromDatahub, allClamsFetchedFromWallet);

        var (totalHourConsumption, averageHourConsumption) = await allConsumptionFetchedFromDatahub;
        var allClaims = (await allClamsFetchedFromWallet)?.Result.ToList() ?? new List<Claim>();

        foreach (var claim in allClaims)
        {
            _logger.LogInformation(
                    "Fetched claims: Quantity {Quantity}, Consumption {StartConsumption} {EndConsumption}, Production {StartProduction} {EndProduction}",
                    claim.Quantity, claim.ConsumptionCertificate.Start, claim.ConsumptionCertificate.End, claim.ProductionCertificate.Start, claim.ProductionCertificate.End);
        }
        var filteredClaims = allClaims.Where(claim => claim.IsTrialClaim() == isTrial).ToList();

        return (totalHourConsumption, averageHourConsumption, claims: filteredClaims);
    }
}
