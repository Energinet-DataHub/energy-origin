using System;
using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Domain;
using API.Transfer.Api.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;

namespace API.ReportGenerator.Processing;

public interface ISvgDataProcessor
{
    List<HourlyEnergy> Format(List<ConsumptionHour> averageConsumptionHours, List<Claim> claims);
}

public class SvgDataProcessor : ISvgDataProcessor
{
    public List<HourlyEnergy> Format(List<ConsumptionHour> averageConsumptionHours, List<Claim> claims)
    {
        var averageClaimHours = (from c in claims
            where c.ProductionCertificate.Start - c.ConsumptionCertificate.Start <= UnixTimestamp.SecondsPerHour
            group (double)c.Quantity by DateTimeOffset.FromUnixTimeSeconds(c.ProductionCertificate.Start).Hour
            into g
            select new { g.Key, avg = g.Average(x => x) }).ToList();

        var hours = Enumerable.Range(0, 24);

        var result = new List<HourlyEnergy>();
        foreach (var hour in hours)
        {
            var averageConsumptionHour = (double)(averageConsumptionHours.FirstOrDefault(x => x.HourOfDay == hour) == null ? 0 : averageConsumptionHours.First(x => x.HourOfDay == hour).KwhQuantity * 1000);

            var matched = averageClaimHours.FirstOrDefault(x => x.Key == hour)?.avg ?? 0;
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
    public static double MaxStacked(IReadOnlyList<HourlyEnergy> h)
        => h.Max(x => Math.Max(x.Consumption, x.Matched + x.Unmatched + x.Overmatched));
}
