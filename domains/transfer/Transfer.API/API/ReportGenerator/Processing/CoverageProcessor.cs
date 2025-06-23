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

public class CoverageProcessor : ICoverageProcessor
{
    private readonly ILogger<CoverageProcessor> _logger;

    public CoverageProcessor(ILogger<CoverageProcessor> logger)
    {
        _logger = logger;
    }

    public CoveragePercentage Calculate(IReadOnlyList<Claim> claims, IReadOnlyList<ConsumptionHour> consumption, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var totalConsumption = (double)consumption.Sum(x => x.KwhQuantity * 1000);

        _logger.LogInformation("TotalConsumption: " + totalConsumption);

        if (totalConsumption == 0)
        {
            return new CoveragePercentage(100,
                100,
                100,
                (endDate - startDate).Duration() >= TimeSpan.FromDays(30) ? 100 : null,
                (endDate - startDate).Duration() >= TimeSpan.FromDays(365) ? 100 : null);
        }

        var hourly = (double)claims.Where(x => x.ProductionCertificate.Start == x.ConsumptionCertificate.Start)
            .Sum(x => x.Quantity);
        var hourlyPercentage = (hourly / totalConsumption) * 100;

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

        _logger.LogInformation("hourlyPercentage: " + hourlyPercentage);
        _logger.LogInformation("dailyPercentage: " + dailyPercentage);
        _logger.LogInformation("weeklyPercentage: " + weeklyPercentage);
        _logger.LogInformation("monthlyPercentage: " + monthlyPercentage);
        _logger.LogInformation("yearlyPercentage: " + yearlyPercentage);

        return new CoveragePercentage(hourlyPercentage,
            dailyPercentage,
            weeklyPercentage,
            monthlyPercentage,
            yearlyPercentage);
    }
}

public record CoveragePercentage(
    double HourlyPercentage,
    double DailyPercentage,
    double WeeklyPercentage,
    double? MonthlyPercentage,
    double? YearlyPercentage);
