using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;

namespace API.ReportGenerator.Infrastructure;

public interface IEnergyDataFetcher
{
    Task<(List<ConsumptionHour> totalHourConsumption, List<ConsumptionHour> averageHourConsumption, List<Claim> claims)> GetAsync(OrganizationId orgId, DateTimeOffset from, DateTimeOffset to, bool isTrial, CancellationToken ct = default);
}

public sealed class EnergyDataFetcher : IEnergyDataFetcher
{
    private readonly IConsumptionService _consumptionService;
    private readonly IWalletClient _walletClient;

    public EnergyDataFetcher(
        IConsumptionService consumptionService,
        IWalletClient walletClient)
    {
        _consumptionService = consumptionService ?? throw new ArgumentNullException(nameof(consumptionService));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
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

        // Filter claims based on trial status
        var filteredClaims = allClaims.Where(claim => IsTrialClaim(claim) == isTrial).ToList();

        return (totalHourConsumption, averageHourConsumption, claims: filteredClaims);
    }

    private static bool IsTrialClaim(Claim claim)
    {
        // A claim is trial only when BOTH certificates have IsTrial=true
        // Mixed states (true/false) are impossible from the upstream service (Vault)
        var productionIsTrial = claim.ProductionCertificate.Attributes.TryGetValue("IsTrial", out var prodVal) &&
                               string.Equals(prodVal, "true", StringComparison.OrdinalIgnoreCase);

        var consumptionIsTrial = claim.ConsumptionCertificate.Attributes.TryGetValue("IsTrial", out var consVal) &&
                                string.Equals(consVal, "true", StringComparison.OrdinalIgnoreCase);

        return productionIsTrial && consumptionIsTrial;
    }
}
