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

        var consTask = _consumptionService.GetAverageHourlyConsumption(orgId, from, to, ct);
        var hourlyClaimsTask = _walletClient.GetClaims(orgId.Value, from, to, TimeMatch.Hourly, ct);
        var allClaimsTask = _walletClient.GetClaims(orgId.Value, from, to, TimeMatch.All, ct);

        await Task.WhenAll(consTask, hourlyClaimsTask, allClaimsTask);

        var consumption = (await consTask)
            .OrderBy(x => x.HourOfDay)
            .Select(x => new DataPoint(x.HourOfDay, (double)x.KwhQuantity));

        var strictProduction = (await hourlyClaimsTask)?.Result
            .Select(c => new DataPoint(
                DateTimeOffset.FromUnixTimeSeconds(c.UpdatedAt).Hour,
                c.Quantity))
            ?? Enumerable.Empty<DataPoint>();

        var allProduction = (await allClaimsTask)?.Result
            .Select(c => new DataPoint(
                DateTimeOffset.FromUnixTimeSeconds(c.UpdatedAt).Hour,
                c.Quantity))
            ?? Enumerable.Empty<DataPoint>();

        return (consumption, strictProduction, allProduction);
    }
}
