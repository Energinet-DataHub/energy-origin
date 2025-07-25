using System;
using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Domain;
using API.Transfer.Api.Services;
using EnergyOrigin.WalletClient;
using Microsoft.Extensions.Logging;

namespace API.ReportGenerator.Processing;

public interface ISvgDataProcessor
{
    List<HourlyEnergy> Format(List<ConsumptionHour> averageConsumptionHours, List<Claim> claims);
}

public class SvgDataProcessor(ILogger<SvgDataProcessor> logger) : ISvgDataProcessor
{
    public List<HourlyEnergy> Format(List<ConsumptionHour> averageConsumptionHours, List<Claim> claims)
    {
        foreach (var consumption in averageConsumptionHours)
        {
            logger.LogInformation("SvgDataProcessor consumption {Hour}, {KWH}", consumption.HourOfDay, consumption.KwhQuantity); ;
        }

        foreach (var claim in claims)
        {
            logger.LogInformation("SvgDataProcessor claim {Date}, {Quantity}", DateTimeOffset.FromUnixTimeSeconds(claim.ProductionCertificate.Start), claim.Quantity);
        }

        // Step 1: Group by day and hour, summing matched claims per (day, hour)
        var dailyHourTotals =
            from c in claims
            where c.ProductionCertificate.Start == c.ConsumptionCertificate.Start
            let dt = DateTimeOffset.FromUnixTimeSeconds(c.ProductionCertificate.Start)
            let day = dt.Date
            let hour = dt.Hour
            group (double)c.Quantity by new { day, hour } into g
            select new { g.Key.day, g.Key.hour, total = g.Sum(x => x) };

        // Step 2: Count distinct days
        var dayCount = dailyHourTotals.Select(x => x.day).Distinct().Count();

        // Step 3: Average those totals per hour of day
        var averageClaimHours =
            (from entry in dailyHourTotals
             group entry.total by entry.hour into g
             select new { Hour = g.Key, Avg = g.Sum() / dayCount })
            .ToList();


        foreach (var averageClaimHour in averageClaimHours)
        {
            logger.LogInformation("SvgDataProcessor average claim {Hour}, {AVG}", averageClaimHour.Hour, averageClaimHour.Avg);
        }

        var hours = Enumerable.Range(0, 24);

        var result = new List<HourlyEnergy>();
        foreach (var hour in hours)
        {
            var averageConsumptionHour = (double)(averageConsumptionHours.FirstOrDefault(x => x.HourOfDay == hour) == null ? 0 : averageConsumptionHours.First(x => x.HourOfDay == hour).KwhQuantity * 1000);

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

        foreach (var elem in result)
        {
            logger.LogInformation("SvgDataProcessor calculated {Hour}, {Consumption}, {Matched}", elem.Hour, elem.Consumption, elem.Matched);
        }

        return result;
    }

    public static double MaxStackedWattHours(IReadOnlyList<HourlyEnergy> h)
        => h.Max(x => Math.Max(x.Consumption, x.Matched + x.Unmatched + x.Overmatched));

    public static double MinStackedWattHours(IReadOnlyList<HourlyEnergy> h)
        => h.Min(x => Math.Min(x.Consumption, x.Matched + x.Unmatched + x.Overmatched));
}
