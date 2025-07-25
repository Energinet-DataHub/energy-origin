using System;
using System.Collections.Generic;
using API.ReportGenerator.Processing;
using API.Transfer.Api.Services;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.ReportGenerator.Processing;

public class CoverageProcessorTests
{
    [Fact]
    public void Calculate_WhenHalfConsumptionDistributedOnCoverageTypes_Expect50YearlyPercentage()
    {
        var now = DateTimeOffset.Now;
        var hourly = GenerateClaims(2, now, now);
        var daily1HourApart = GenerateClaims(2, now, now.AddHours(-1));
        var daily24HoursApart = GenerateClaims(2, now, now.AddHours(-24));
        var weekly = GenerateClaims(2, now, now.AddDays(-7));
        var monthly = GenerateClaims(2, now, now.AddDays(30));
        var yearly = GenerateClaims(2, now, now.AddDays(365));

        var claims = new List<Claim>();
        claims.AddRange(hourly);
        claims.AddRange(daily1HourApart);
        claims.AddRange(daily24HoursApart);
        claims.AddRange(weekly);
        claims.AddRange(monthly);
        claims.AddRange(yearly);

        var consumption = GenerateConsumption();

        var sut = new CoverageProcessor(Substitute.For<ILogger<CoverageProcessor>>());

        var result = sut.Calculate(claims, consumption, now, now.AddYears(1));

        Assert.Equal(8.3, Math.Round(result.HourlyPercentage, 1));
        Assert.Equal(25, Math.Round(result.DailyPercentage));
        Assert.Equal(33.3, Math.Round(result.WeeklyPercentage, 1));
        Assert.Equal(41.7, Math.Round(result.MonthlyPercentage!.Value, 1));
        Assert.Equal(50, Math.Round(result.YearlyPercentage!.Value));
    }

    [Fact]
    public void Calculate_WhenHalfConsumptionOnHourly_Expect50HourlyPercentageAndOtherCoverages()
    {
        var now = DateTimeOffset.Now;
        var hourly = GenerateClaims(12, now, now);

        var claims = new List<Claim>();
        claims.AddRange(hourly);

        var consumption = GenerateConsumption();

        var sut = new CoverageProcessor(Substitute.For<ILogger<CoverageProcessor>>());

        var result = sut.Calculate(claims, consumption, now, now.AddYears(1));

        Assert.Equal(50, result.HourlyPercentage);
        Assert.Equal(50, result.DailyPercentage);
        Assert.Equal(50, result.WeeklyPercentage);
        Assert.Equal(50, result.MonthlyPercentage!.Value);
        Assert.Equal(50, result.YearlyPercentage!.Value);
    }

    [Fact]
    public void Calculate_WhenNoClaimsProvided_ReturnsZero()
    {
        var now = DateTimeOffset.Now;

        var consumption = GenerateConsumption();

        var sut = new CoverageProcessor(Substitute.For<ILogger<CoverageProcessor>>());

        var result = sut.Calculate(new List<Claim>(), consumption, now, now.AddYears(1));

        Assert.Equal(0, result.HourlyPercentage);
        Assert.Equal(0, result.DailyPercentage);
        Assert.Equal(0, result.WeeklyPercentage);
        Assert.Equal(0, result.MonthlyPercentage!.Value);
        Assert.Equal(0, result.YearlyPercentage!.Value);
    }

    [Fact]
    public void Calculate_WhenTotalConsumptionIsZero_ReturnsHundredPercent()
    {
        var now = DateTimeOffset.Now;

        var sut = new CoverageProcessor(Substitute.For<ILogger<CoverageProcessor>>());

        var result = sut.Calculate(new List<Claim>(), new List<ConsumptionHour>(), now, now.AddYears(1));

        Assert.Equal(100, result.HourlyPercentage);
        Assert.Equal(100, result.DailyPercentage);
        Assert.Equal(100, result.WeeklyPercentage);
        Assert.Equal(100, result.MonthlyPercentage!.Value);
        Assert.Equal(100, result.YearlyPercentage!.Value);
    }

    [Fact]
    public void Calculate_WhenEndDateIsLessThan1Month_ExpectYearlyAndMonthlyPercentageNull()
    {
        var now = DateTimeOffset.Now;
        var hourly = GenerateClaims(2, now, now);
        var daily1HourApart = GenerateClaims(2, now, now.AddHours(-1));
        var daily24HoursApart = GenerateClaims(2, now, now.AddHours(-24));
        var weekly = GenerateClaims(2, now, now.AddDays(-7));
        var monthly = GenerateClaims(2, now, now.AddDays(30));
        var yearly = GenerateClaims(2, now, now.AddDays(365));

        var claims = new List<Claim>();
        claims.AddRange(hourly);
        claims.AddRange(daily1HourApart);
        claims.AddRange(daily24HoursApart);
        claims.AddRange(weekly);
        claims.AddRange(monthly);
        claims.AddRange(yearly);

        var consumption = GenerateConsumption();

        var sut = new CoverageProcessor(Substitute.For<ILogger<CoverageProcessor>>());

        var result = sut.Calculate(claims, consumption, now, now.AddDays(25));

        Assert.Null(result.MonthlyPercentage);
        Assert.Null(result.YearlyPercentage);
    }

    [Fact]
    public void Calculate_WhenEndDateIsLessThan1Year_ExpectYearlyPercentageNullAndMonthlyPercentageNotNull()
    {
        var now = DateTimeOffset.Now;
        var hourly = GenerateClaims(2, now, now);
        var daily1HourApart = GenerateClaims(2, now, now.AddHours(-1));
        var daily24HoursApart = GenerateClaims(2, now, now.AddHours(-24));
        var weekly = GenerateClaims(2, now, now.AddDays(-7));
        var monthly = GenerateClaims(2, now, now.AddDays(30));
        var yearly = GenerateClaims(2, now, now.AddDays(365));

        var claims = new List<Claim>();
        claims.AddRange(hourly);
        claims.AddRange(daily1HourApart);
        claims.AddRange(daily24HoursApart);
        claims.AddRange(weekly);
        claims.AddRange(monthly);
        claims.AddRange(yearly);

        var consumption = GenerateConsumption();

        var sut = new CoverageProcessor(Substitute.For<ILogger<CoverageProcessor>>());

        var result = sut.Calculate(claims, consumption, now, now.AddDays(360));

        Assert.NotNull(result.MonthlyPercentage);
        Assert.Null(result.YearlyPercentage);
    }

    private List<ConsumptionHour> GenerateConsumption()
    {
        var consumption = new List<ConsumptionHour>();
        for (int i = 0; i < 24; i++)
        {
            var hour = new ConsumptionHour(i)
            {
                KwhQuantity = 1
            };

            consumption.Add(hour);
        }

        return consumption;
    }

    private List<Claim> GenerateClaims(int count, DateTimeOffset consumptionStart, DateTimeOffset productionStart)
    {
        var claims = new List<Claim>();
        var attributes = new Dictionary<string, string>();

        for (int i = 0; i < count; i++)
        {
            var claim = new Claim
            {
                ClaimId = Guid.NewGuid(),
                Quantity = 1000,
                UpdatedAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
                ConsumptionCertificate = new ClaimedCertificate
                {
                    Start = consumptionStart.ToUnixTimeSeconds(),
                    End = consumptionStart.AddHours(1).ToUnixTimeSeconds(),
                    FederatedStreamId = new FederatedStreamId
                    {
                        Registry = "Narnia",
                        StreamId = Guid.NewGuid()
                    },
                    GridArea = "DK1",
                    Attributes = attributes
                },
                ProductionCertificate = new ClaimedCertificate
                {
                    Start = productionStart.ToUnixTimeSeconds(),
                    End = productionStart.AddHours(1).ToUnixTimeSeconds(),
                    FederatedStreamId = new FederatedStreamId
                    {
                        Registry = "Narnia",
                        StreamId = Guid.NewGuid()
                    },
                    GridArea = "DK1",
                    Attributes = attributes
                }
            };
            claims.Add(claim);
        }

        return claims;
    }
}
