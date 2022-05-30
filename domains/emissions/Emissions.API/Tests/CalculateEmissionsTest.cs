using System;
using System.Collections.Generic;
using System.Linq;
using API.Helpers;
using API.Models;
using API.Services;
using Xunit;

namespace Tests;

public class CalculateEmissionsTest
{
    readonly DateSetFactory dateSetFactory = new();
    
    [Theory]
    [InlineData(Aggregation.Total)]
    [InlineData(Aggregation.Actual)]
    [InlineData(Aggregation.Hour)]
    [InlineData(Aggregation.Day)]
    [InlineData(Aggregation.Month)]
    [InlineData(Aggregation.Year)]
    public void EmissionsAndMeasurements_CalculateTotalEmission_TotalAnRelativeEmission(Aggregation aggregation)
    {
        var dateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2021, 1, 2, 1, 59, 59, DateTimeKind.Utc);
        var timeSeries = dateSetFactory.CreateTimeSeries();
        var emissions = dateSetFactory.CreateEmissions();
     

        var sut = new EmissionsCalculator();

        var result = sut.CalculateEmission(emissions, timeSeries, dateFrom.ToUnixTime(),
            dateTo.ToUnixTime(), aggregation);

        Assert.NotNull(result);
        var emissionsEnumerable = GetExpectedEmissions(aggregation, dateFrom, dateTo);
        Assert.Equal(emissionsEnumerable.Select(_ => _.Total.Co2), result.Select(_ => _.Total.Co2));
        Assert.Equal(emissionsEnumerable.Select(_ => _.Relative.Co2), result.Select(_ => _.Relative.Co2));
        Assert.Equal(emissionsEnumerable.Select(_ => _.DateFrom), result.Select(_ => _.DateFrom));
        Assert.Equal(emissionsEnumerable.Select(_ => _.DateTo), result.Select(_ => _.DateTo));
    }

    private IEnumerable<Emissions> GetExpectedEmissions(Aggregation aggregation, DateTime dateFrom, DateTime dateTo)
    {
        switch (aggregation)
        {
            case Aggregation.Total:
                return new List<Emissions>()
                {
                    new Emissions
                    {
                        DateFrom = dateFrom.ToUnixTime(),
                        DateTo = dateTo.ToUnixTime(),
                        Relative = new Relative{Co2 = 122.4514f},
                        Total = new Total() {Co2 =  481.234f }
                    }
                };
            case Aggregation.Actual:
            case Aggregation.Hour:
                return new List<Emissions>()
                {
                    new Emissions
                    {
                        DateFrom = dateFrom.ToUnixTime(),
                        DateTo = dateFrom.AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        Relative = new Relative{Co2 = 124f},
                        Total = new Total() {Co2 =  153.016f }
                    },
                    new Emissions
                    {
                        DateFrom = dateFrom.AddHours(1).ToUnixTime(),
                        DateTo = dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        Relative = new Relative{Co2 = 234f},
                        Total = new Total() {Co2 =  56.628f }
                    },
                    new Emissions
                    {
                        DateFrom = dateFrom.AddHours(2).ToUnixTime(),
                        DateTo = dateFrom.AddHours(2).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        Relative = new Relative{Co2 =85f},
                        Total = new Total() {Co2 =  55.59f }
                    },
                    new Emissions
                    {
                        DateFrom = dateFrom.AddHours(3).ToUnixTime(),
                        DateTo = dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        Relative = new Relative{Co2 = 120f},
                        Total = new Total() {Co2 =  216f }
                    },
                };
            case Aggregation.Day:
                return new List<Emissions>()
                {
                    new Emissions
                    {
                        DateFrom = dateFrom.ToUnixTime(),
                        DateTo = dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        Relative = new Relative { Co2 = 142.03523f },
                        Total = new Total() { Co2 = 209.644f }
                    },
                    new Emissions
                    {
                        DateFrom = dateFrom.AddHours(2).ToUnixTime(),
                        DateTo = dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        Relative = new Relative { Co2 = 110.67237f },
                        Total = new Total() { Co2 = 271.59f }
                    }
                };
            case Aggregation.Month:
            case Aggregation.Year:
                return new List<Emissions>()
                {
                    new Emissions
                    {
                        DateFrom = dateFrom.ToUnixTime(),
                        DateTo = dateTo.ToUnixTime(),
                        Relative = new Relative{Co2 = 122.4514f},
                        Total = new Total() {Co2 =  481.234f }
                    }
                };
            default:
                return new List<Emissions>();
        }

        
    }
}