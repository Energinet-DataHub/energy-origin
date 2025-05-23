using System;
using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Domain;

namespace API.ReportGenerator.Processing;

public static class EnergyDataProcessor
{
    public static IReadOnlyList<HourlyEnergy> ToHourly(
        IEnumerable<DataPoint> consumption,
        IEnumerable<DataPoint> production)
    {
        var cons = consumption.ToLookup(d => d.Timestamp.Hour, d => d.Value);
        var prod = production.ToLookup(d => d.Timestamp.Hour, d => d.Value);

        return Enumerable.Range(0, 24)
            .Select(h =>
            {
                var c = cons[h].FirstOrDefault();
                var p = prod[h].DefaultIfEmpty().Average();
                return new HourlyEnergy(
                    Hour: h,
                    Consumption: c,
                    Matched: Math.Min(c, p),
                    Unmatched: Math.Max(0, c - p),
                    Overmatched: Math.Max(0, p - c));
            })
            .ToList();
    }

    public static double MaxStacked(IReadOnlyList<HourlyEnergy> h)
        => h.Max(x => Math.Max(x.Consumption, x.Matched + x.Unmatched + x.Overmatched));
}
