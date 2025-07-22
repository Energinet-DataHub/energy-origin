using System;
using System.Collections.Generic;
using System.Linq;
using API.Transfer.Api.Services;
using EnergyOrigin.WalletClient;
using Microsoft.Extensions.Logging;

namespace API.ReportGenerator.Processing;

public interface ICoverageProcessor
{
    CoveragePercentage Calculate(IReadOnlyList<Claim> claims, IReadOnlyList<ConsumptionHour> consumption, DateTimeOffset startDate, DateTimeOffset endDate);
}

public class CoverageProcessor(ILogger<CoverageProcessor> logger) : ICoverageProcessor
{
    public CoveragePercentage Calculate(IReadOnlyList<Claim> claims, IReadOnlyList<ConsumptionHour> consumption, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var totalConsumption = (double)consumption.Sum(x => x.KwhQuantity) * 1000;
        logger.LogInformation("CoveragePercentage - Total consumption = {Total}", totalConsumption);

        if (totalConsumption == 0)
        {
            return new CoveragePercentage(100,
                100,
                100,
                (endDate - startDate).Duration() >= TimeSpan.FromDays(30) ? 100 : null,
                (endDate - startDate).Duration() >= TimeSpan.FromDays(365) ? 100 : null);
        }

        LogDuplicates(claims);

        var hourly = (double)claims.Where(x => x.ProductionCertificate.Start == x.ConsumptionCertificate.Start)
            .Sum(x => x.Quantity);

        logger.LogInformation("CoveragePercentage: - Hourly {Hourly}", hourly);

        var hourlyPercentage = (hourly / totalConsumption) * 100;

        logger.LogInformation("CoveragePercentage: - Hourly percentage {HourlyPercentage}", hourlyPercentage);

        var daily = (double)claims.Where(x =>
            (DateTimeOffset.FromUnixTimeSeconds(x.ProductionCertificate.Start) - DateTimeOffset.FromUnixTimeSeconds(x.ConsumptionCertificate.Start)).Duration() <= TimeSpan.FromDays(1))
            .Sum(x => x.Quantity);
        var dailyPercentage = (daily / totalConsumption) * 100;

        var weekly = (double)claims.Where(x =>
                (DateTimeOffset.FromUnixTimeSeconds(x.ProductionCertificate.Start) - DateTimeOffset.FromUnixTimeSeconds(x.ConsumptionCertificate.Start)).Duration() <= TimeSpan.FromDays(7))
            .Sum(x => x.Quantity);
        var weeklyPercentage = (weekly / totalConsumption) * 100;

        double? monthlyPercentage = null;
        if ((endDate - startDate).Duration() >= TimeSpan.FromDays(30))
        {
            var monthly = (double)claims.Where(x =>
                    (DateTimeOffset.FromUnixTimeSeconds(x.ProductionCertificate.Start) - DateTimeOffset.FromUnixTimeSeconds(x.ConsumptionCertificate.Start)).Duration() <= TimeSpan.FromDays(30))
                .Sum(x => x.Quantity);
            monthlyPercentage = (monthly / totalConsumption) * 100;
        }

        double? yearlyPercentage = null;
        if ((endDate - startDate).Duration() >= TimeSpan.FromDays(365))
        {
            var yearly = (double)claims.Where(x =>
                (DateTimeOffset.FromUnixTimeSeconds(x.ProductionCertificate.Start) - DateTimeOffset.FromUnixTimeSeconds(x.ConsumptionCertificate.Start)).Duration() <= TimeSpan.FromDays(365))
            .Sum(x => x.Quantity);
            yearlyPercentage = (yearly / totalConsumption) * 100;
        }

        return new CoveragePercentage(hourlyPercentage,
            dailyPercentage,
            weeklyPercentage,
            monthlyPercentage,
            yearlyPercentage);
    }

    private void LogDuplicates(IReadOnlyList<Claim> claims)
    {
        var hourlyClaims = claims.Where(x => x.ProductionCertificate.Start == x.ConsumptionCertificate.Start);
        var duplicateGroups = hourlyClaims
            .GroupBy(x => x.ProductionCertificate.Start)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Count != 0)
        {
            foreach (var group in duplicateGroups)
            {
                logger.LogInformation("Duplicate hourly claims found for start time {StartTime}, count: {Count}",
                    DateTimeOffset.FromUnixTimeSeconds(group.Key), group.Count());

                foreach (var claim in group.OrderByDescending(c => c.Quantity))
                {
                    logger.LogWarning("Duplicate claim: ID: {ClaimId}, Quantity: {Quantity}",
                        claim.GetHashCode(), claim.Quantity);
                }
            }
        }
    }
}

public record CoveragePercentage(
    double HourlyPercentage,
    double DailyPercentage,
    double WeeklyPercentage,
    double? MonthlyPercentage,
    double? YearlyPercentage);
