using API.ReportGenerator.Processing;
using API.Transfer.Api.Services;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace API.UnitTests.ReportGenerator.Processing;

public class SvgDataProcessorTests
{
    [Fact]
    public void Format_WhenHalfQuantityIs500AndOtherHalfIs1000_Expect750AverageMatched()
    {
        var claims = GenerateClaims(24, 500);
        var claim2 = GenerateClaims(24, 1000);

        claims.AddRange(claim2);
        var consumptionAvg = GenerateAvgConsumption(1);

        var sut = new SvgDataProcessor();

        var hourlyEnergy = sut.Format(consumptionAvg, claims);

        Assert.Equal(24, hourlyEnergy.Count);
        foreach (var hourly in hourlyEnergy)
        {
            Assert.Equal(750, hourly.Matched);
            Assert.Equal(250, hourly.Unmatched);
            Assert.Equal(1000, hourly.Consumption);
        }
    }

    [Fact]
    public void Format_WhenAverageIsTheSame_ExpectTheSame()
    {
        var claims = GenerateClaims(60, 1000);
        var consumptionAvg = GenerateAvgConsumption(2);

        var sut = new SvgDataProcessor();

        var hourlyEnergy = sut.Format(consumptionAvg, claims);

        Assert.Equal(24, hourlyEnergy.Count);
        foreach (var hourly in hourlyEnergy)
        {
            Assert.Equal(1000, hourly.Matched);
            Assert.Equal(1000, hourly.Unmatched);
            Assert.Equal(2000, hourly.Consumption);
        }
    }

    [Fact]
    public void Format_WhenClamsMissingAnHour_ExpectZero()
    {
        var hourMissing = 3;
        var claims = GenerateClaims(60, 1000);
        claims = claims.Where(x => DateTimeOffset.FromUnixTimeSeconds(x.ProductionCertificate.Start).Hour != hourMissing).ToList();

        var consumptionAvg = GenerateAvgConsumption(2);

        var sut = new SvgDataProcessor();

        var hourlyEnergy = sut.Format(consumptionAvg, claims);

        Assert.Equal(24, hourlyEnergy.Count);
        Assert.Equal(0, hourlyEnergy[hourMissing].Matched);
        Assert.Equal(2000, hourlyEnergy[hourMissing].Unmatched);
        Assert.Equal(2000, hourlyEnergy[hourMissing].Consumption);
    }

    [Fact]
    public void Format_WhenConsumptionMissingAnHour_ExpectZero()
    {
        var hourMissing = 3;
        var claims = GenerateClaims(60, 1000);
        var consumptionAvg = GenerateAvgConsumption(2);
        consumptionAvg = consumptionAvg.Where(x => x.HourOfDay != hourMissing).ToList();

        var sut = new SvgDataProcessor();

        var hourlyEnergy = sut.Format(consumptionAvg, claims);

        Assert.Equal(24, hourlyEnergy.Count);
        Assert.Equal(1000, hourlyEnergy[hourMissing].Matched);
        Assert.Equal(0, hourlyEnergy[hourMissing].Unmatched);
        Assert.Equal(1000, hourlyEnergy[hourMissing].Consumption);
    }

    [Fact]
    public void Format_WhenConsumptionAndClaimMissingAnHour_ExpectZero()
    {
        var hourMissing = 3;
        var claims = GenerateClaims(60, 1000);
        claims = claims.Where(x => DateTimeOffset.FromUnixTimeSeconds(x.ProductionCertificate.Start).Hour != hourMissing).ToList();
        var consumptionAvg = GenerateAvgConsumption(2);
        consumptionAvg = consumptionAvg.Where(x => x.HourOfDay != hourMissing).ToList();

        var sut = new SvgDataProcessor();

        var hourlyEnergy = sut.Format(consumptionAvg, claims);

        Assert.Equal(24, hourlyEnergy.Count);
        Assert.Equal(0, hourlyEnergy[hourMissing].Matched);
        Assert.Equal(0, hourlyEnergy[hourMissing].Unmatched);
        Assert.Equal(0, hourlyEnergy[hourMissing].Consumption);
    }
    [Fact]
    public void Format_WhenProductionAndConsumptionStartDifferByOneHour_MatchedIsZero()
    {
        // Arrange
        var claims = new List<Claim>();
        var attributes = new Dictionary<string, string>();

        var now = DateTimeOffset.Now;
        var baseDateOffset = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);

        for (int i = 0; i < 24; i++)
        {
            var consumptionStart = baseDateOffset.AddHours(i + 1).ToUnixTimeSeconds();
            var productionStart = baseDateOffset.AddHours(i).ToUnixTimeSeconds(); // 1 hour before consumption

            var claim = new Claim
            {
                ClaimId = Guid.NewGuid(),
                Quantity = 1000,
                UpdatedAt = now.ToUnixTimeSeconds(),
                ConsumptionCertificate = new ClaimedCertificate
                {
                    Start = consumptionStart,
                    End = consumptionStart + 3600,
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
                    Start = productionStart,
                    End = productionStart + 3600,
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

        var consumptionAvg = Enumerable.Range(0, 24).Select(h => new ConsumptionHour(h)
        {
            KwhQuantity = 1
        }).ToList();

        var sut = new SvgDataProcessor();

        // Act
        var result = sut.Format(consumptionAvg, claims);

        // Assert
        foreach (var hourly in result)
        {
            Assert.Equal(0, hourly.Matched);
            Assert.Equal(1000, hourly.Unmatched);
            Assert.Equal(1000, hourly.Consumption);
        }
    }

    private List<ConsumptionHour> GenerateAvgConsumption(decimal kwhQuantity)
    {
        return Enumerable.Range(0, 24).Select(h => new ConsumptionHour(h)
        {
            KwhQuantity = kwhQuantity
        }).ToList();
    }

    private List<Claim> GenerateClaims(int count, uint quantity)
    {
        var claims = new List<Claim>();
        var attributes = new Dictionary<string, string>();

        for (int i = 0; i < count; i++)
        {
            var claim = new Claim
            {
                ClaimId = Guid.NewGuid(),
                Quantity = quantity,
                UpdatedAt = DateTimeOffset.Now.AddHours(i).ToUnixTimeSeconds(),
                ConsumptionCertificate = new ClaimedCertificate
                {
                    Start = DateTimeOffset.Now.AddHours(i).ToUnixTimeSeconds(),
                    End = DateTimeOffset.Now.AddHours(i + 1).ToUnixTimeSeconds(),
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
                    Start = DateTimeOffset.Now.AddHours(i).ToUnixTimeSeconds(),
                    End = DateTimeOffset.Now.AddHours(i + 1).ToUnixTimeSeconds(),
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
