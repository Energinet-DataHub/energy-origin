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

public sealed class EnergyDataFetcher(IConsumptionService consumption, IWalletClient wallet)
{
    public async Task<(IEnumerable<DataPoint> Consumption,
        IEnumerable<DataPoint> StrictProduction,
        IEnumerable<DataPoint> AllProduction)> GetAsync(
        OrganizationId orgId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var consTask = consumption.GetAverageHourlyConsumption(orgId, from, to, ct);
        var hourlyClaimsTask = wallet.GetClaims(orgId.Value, from, to, TimeMatch.Hourly, ct);
        var allClaimsTask = wallet.GetClaims(orgId.Value, from, to, TimeMatch.All, ct);

        await Task.WhenAll(consTask, hourlyClaimsTask, allClaimsTask);

        var cons = (await consTask)
            .OrderBy(x => x.HourOfDay)
            .Select(x => new DataPoint(from.Date.AddHours(x.HourOfDay), (double)x.KwhQuantity));

        // Process strict hourly claims
        var strictProd = ((await hourlyClaimsTask)?.Result ?? Enumerable.Empty<Claim>())
            .GroupBy(c => DateTimeOffset.FromUnixTimeSeconds(c.UpdatedAt).Hour)
            .SelectMany(g => g.Select(c => new DataPoint(from.Date.AddHours(g.Key), c.Quantity)));

        // Process all claims (including non-hourly)
        var allProd = ((await allClaimsTask)?.Result ?? Enumerable.Empty<Claim>())
            .GroupBy(c => DateTimeOffset.FromUnixTimeSeconds(c.UpdatedAt).Hour)
            .SelectMany(g => g.Select(c => new DataPoint(from.Date.AddHours(g.Key), c.Quantity)));

        return (cons, strictProd, allProd);
    }
}
