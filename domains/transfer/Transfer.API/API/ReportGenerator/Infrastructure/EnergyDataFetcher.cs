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
        IEnumerable<DataPoint> Production)> GetAsync(
        OrganizationId orgId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var consTask = consumption.GetTotalHourlyConsumption(orgId, from, to, ct);
        var claimsTask = wallet.GetClaims(orgId.Value, from, to, ct);

        await Task.WhenAll(consTask, claimsTask);

        var cons = (await consTask)
            .OrderBy(x => x.HourOfDay)
            .Select(x => new DataPoint(from.Date.AddHours(x.HourOfDay),
                (double)x.KwhQuantity));

        var prod = ((await claimsTask)?.Result ?? Enumerable.Empty<Claim>())
            .GroupBy(c => DateTimeOffset.FromUnixTimeSeconds(c.UpdatedAt).Hour)
            .SelectMany(g => g.Select(c => new DataPoint(from.Date.AddHours(g.Key),
                c.Quantity)));

        return (cons, prod);
    }
}
