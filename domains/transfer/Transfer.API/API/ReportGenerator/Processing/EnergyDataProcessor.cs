using System;
using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Domain;

namespace API.ReportGenerator.Processing;

public static class EnergyDataProcessor
{
    public static IReadOnlyList<HourlyEnergy> ToHourly(
        IEnumerable<DataPoint> consumption,
        IEnumerable<DataPoint> strictProduction,
        IEnumerable<DataPoint> allProduction)
    {
        var cons = consumption.ToLookup(d => d.Timestamp.Hour, d => d.Value);
        var strictProd = strictProduction.ToLookup(d => d.Timestamp.Hour, d => d.Value);
        var allProd = allProduction.ToLookup(d => d.Timestamp.Hour, d => d.Value);

        return Enumerable.Range(0, 24)
            .Select(h =>
            {
                var c = cons[h].DefaultIfEmpty().Average();
                var sp = strictProd[h].DefaultIfEmpty().Average();
                var ap = allProd[h].DefaultIfEmpty().Average();

                var matched = Math.Min(c, sp); // Matched uses strict hourly production
                var unmatched = Math.Max(0, c - sp);
                var overmatched = Math.Max(0, ap - c); // Overmatched uses all production

                return new HourlyEnergy(
                    Hour: h,
                    Consumption: c,
                    Matched: matched,
                    Unmatched: unmatched,
                    Overmatched: overmatched);
            })
            .ToList();
    }

    public static double MaxStacked(IReadOnlyList<HourlyEnergy> h)
        => h.Max(x => Math.Max(x.Consumption, x.Matched + x.Unmatched + x.Overmatched));
}
