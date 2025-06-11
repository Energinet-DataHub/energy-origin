using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ReportGenerator.Domain;
using API.Transfer.Api.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;

namespace API.ReportGenerator.Infrastructure;

public interface IEnergyDataFetcher
{
    Task<(IEnumerable<DataPoint> Consumption,
            IEnumerable<DataPoint> StrictProduction,
            IEnumerable<DataPoint> AllProduction)>
        GetAsync(OrganizationId orgId, DateTimeOffset from, DateTimeOffset to,
            CancellationToken ct = default);
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

    public async Task<(IEnumerable<DataPoint> Consumption,
            IEnumerable<DataPoint> StrictProduction,
            IEnumerable<DataPoint> AllProduction)>
        GetAsync(
            OrganizationId orgId,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken ct = default)
    {
        if (orgId is null) throw new ArgumentNullException(nameof(orgId));

        var allConsumptionFetchedFromDatahub = _consumptionService.GetAverageHourlyConsumption(orgId, from, to, ct);
        var allClamsFetchedFromWallet = _walletClient.GetClaimsAsync(orgId.Value, from, to, TimeMatch.All, ct);

        await Task.WhenAll(allConsumptionFetchedFromDatahub, allClamsFetchedFromWallet);

        var consumption = (await allConsumptionFetchedFromDatahub)
            .OrderBy(x => x.HourOfDay)
            .Select(x => new DataPoint(x.HourOfDay, (double)x.KwhQuantity));

        var claimList = (await allClamsFetchedFromWallet)?.Result.ToList() ?? new List<Claim>();

        var strictProduction = new List<DataPoint>(capacity: claimList.Count);
        var allProduction = new List<DataPoint>(capacity: claimList.Count);

        foreach (var claim in claimList)
        {
            var prodTs = UnixTimestamp.Create(claim.ProductionCertificate.Start);
            var consTs = UnixTimestamp.Create(claim.ConsumptionCertificate.Start);
            var delta = consTs.EpochSeconds - prodTs.EpochSeconds;

            var hour = UnixTimestamp.Create(claim.UpdatedAt)
                .ToDateTimeOffset()
                .Hour;
            var dp = new DataPoint(hour, claim.Quantity);

            allProduction.Add(dp);

            if (delta < UnixTimestamp.SecondsPerHour)
            {
                strictProduction.Add(dp);
            }
        }

        return (
            Consumption: consumption,
            StrictProduction: strictProduction,
            AllProduction: allProduction
        );
    }
}
