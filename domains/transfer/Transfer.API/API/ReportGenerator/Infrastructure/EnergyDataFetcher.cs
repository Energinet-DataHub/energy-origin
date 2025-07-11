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

        var filteredClaims = allClaims.Where(claim => claim.IsTrialClaim() == isTrial).ToList();

        return (totalHourConsumption, averageHourConsumption, claims: filteredClaims);
    }
}
