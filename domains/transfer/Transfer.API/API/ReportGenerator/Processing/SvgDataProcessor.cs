using System;
using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Domain;
using API.Transfer.Api.Services;
using EnergyOrigin.WalletClient;

namespace API.ReportGenerator.Processing;

public interface ISvgDataProcessor
{
    List<HourlyEnergy> Format(List<ConsumptionHour> averageConsumptionHours, List<Claim> claims);
}

public class SvgDataProcessor() : ISvgDataProcessor
{
    public List<HourlyEnergy> Format(List<ConsumptionHour> averageConsumptionHours, List<Claim> claims)
    {
        var dailyHourTotals =
            from c in claims
            where c.ProductionCertificate.Start == c.ConsumptionCertificate.Start
            let dt = DateTimeOffset.FromUnixTimeSeconds(c.ProductionCertificate.Start)
            let day = dt.Date
            let hour = dt.Hour
            group (double)c.Quantity by new { day, hour } into grouping
            select new { grouping.Key.day, grouping.Key.hour, total = grouping.Sum(x => x) };

        var averageClaimHours =
            (from entry in dailyHourTotals
             group entry.total by entry.hour into grouping
             select new
             {
                 Hour = grouping.Key,
                 Avg = grouping.Average(x => x)  // Average by number of occurences of the specific hour
             })
            .ToList();

        var hours = Enumerable.Range(0, 24);
        var result = new List<HourlyEnergy>();
        foreach (var hour in hours)
        {
            var averageConsumptionHour = (double)(averageConsumptionHours.FirstOrDefault(x => x.HourOfDay == hour) == null
                    ? 0
                    : averageConsumptionHours.First(x => x.HourOfDay == hour).KwhQuantity * 1000);

            var matched = averageClaimHours.FirstOrDefault(x => x.Hour == hour)?.Avg ?? 0;
            var unmatched = averageConsumptionHour - matched;
            if (unmatched < 0)
            {
                unmatched = 0;
            }

            //overmatch is left out for now
            //consumption is set to matched because Consumption controls the red line. If we have matched more than consumption, the line needs
            //to be drawn over the green area instead of the grey
            var hourly = new HourlyEnergy(hour, matched > averageConsumptionHour ? matched : averageConsumptionHour, matched, unmatched, 0);
            result.Add(hourly);
        }

        return result;
    }

    public static double MaxStackedWattHours(IReadOnlyList<HourlyEnergy> h)
        => h.Max(x => Math.Max(x.Consumption, x.Matched + x.Unmatched + x.Overmatched));

    public static double MinStackedWattHours(IReadOnlyList<HourlyEnergy> h)
        => h.Min(x => Math.Min(x.Consumption, x.Matched + x.Unmatched + x.Overmatched));
}
