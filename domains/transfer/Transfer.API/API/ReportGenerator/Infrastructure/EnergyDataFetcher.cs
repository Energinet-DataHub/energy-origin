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
    Task<(List<ConsumptionHour> consumption, List<Claim> claims)> GetAsync(OrganizationId orgId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
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

    public async Task<(List<ConsumptionHour> consumption, List<Claim> claims)> GetAsync(
        OrganizationId orgId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        if (orgId is null) throw new ArgumentNullException(nameof(orgId));

        var allConsumptionFetchedFromDatahub = _consumptionService.GetAverageHourlyConsumption(orgId, from, to, ct);
        var allClamsFetchedFromWallet = _walletClient.GetClaimsAsync(orgId.Value, from, to, TimeMatch.All, ct);

        await Task.WhenAll(allConsumptionFetchedFromDatahub, allClamsFetchedFromWallet);

        var consumption = await allConsumptionFetchedFromDatahub;
        var claimList = (await allClamsFetchedFromWallet)?.Result.ToList() ?? new List<Claim>();

        return (consumption: consumption, claims: claimList);
    }
}
