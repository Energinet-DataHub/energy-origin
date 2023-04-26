using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using API.Services;
using Tests.Helpers;
using Xunit;

namespace Tests
{
    public sealed class CalculateSourceEmissionShareTests
    {
        [Theory]
        [InlineData(Aggregation.Total)]
        [InlineData(Aggregation.Actual)]
        [InlineData(Aggregation.Hour)]
        [InlineData(Aggregation.Day)]
        [InlineData(Aggregation.Month)]
        [InlineData(Aggregation.Year)]
        public void EmissionSharesAndMeasurements_CalculateTotalEmission_TotalAnRelativeEmission(Aggregation aggregation)
        {
            Environment.SetEnvironmentVariable("RENEWABLESOURCES", "wood,waste,straw,bioGas,solar,windOnshore,windOffshore");
            Environment.SetEnvironmentVariable("WASTERENEWABLESHARE", "55");
            var dateFrom = new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero);
            var dateTo = new DateTimeOffset(2021, 1, 2, 2, 0, 0, TimeSpan.Zero);
            var timeSeries = DataSetFactory.CreateTimeSeries();
            var emissionShares = StaticDataSetFactory.CreateEmissionsShares();
            var calculator = new SourcesCalculator();

            var result = calculator.CalculateSourceEmissions(emissionShares, timeSeries, TimeZoneInfo.Utc, aggregation);

            Assert.NotNull(result);
            var expected = GetExpectedSourceEmissions(aggregation, dateFrom, dateTo).EnergySources;
            Assert.Equal(expected.Select(x => x.Renewable), result.EnergySources.Select(x => x.Renewable));
            Assert.Equal(expected.Select(x => x.Ratios), result.EnergySources.Select(x => x.Ratios));
            Assert.Equal(expected.Select(x => x.DateFrom), result.EnergySources.Select(x => x.DateFrom));
            Assert.Equal(expected.Select(x => x.DateTo), result.EnergySources.Select(x => x.DateTo));
        }

        [Theory]
        [InlineData(Aggregation.Day, 24, "Europe/Copenhagen")]
        [InlineData(Aggregation.Month, 31 * 24, "Europe/Copenhagen")]
        [InlineData(Aggregation.Day, 24, "America/Los_Angeles")]
        [InlineData(Aggregation.Month, 31 * 24, "America/Los_Angeles")]
        [InlineData(Aggregation.Day, 24, "Asia/Kolkata")]
        [InlineData(Aggregation.Month, 31 * 24, "Asia/Kolkata")]
        public void Calculate_AggreatingToOne_WhenAggregationMatchesAmountOfHours(Aggregation aggregation, int amount, string timeZoneId)
        {
            Environment.SetEnvironmentVariable("RENEWABLESOURCES", "wood,waste,straw,bioGas,solar,windOnshore,windOffshore");
            Environment.SetEnvironmentVariable("WASTERENEWABLESHARE", "55");
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var date = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            date = date.Add(-timeZone.GetUtcOffset(date.UtcDateTime));
            var series = DataSetFactory.CreateTimeSeries(startingAt: date, amount: amount);
            var mixes = DataSetFactory.CreateMixSeries(startingAt: date, amount: amount);
            var calculator = new SourcesCalculator();

            var result = calculator.CalculateSourceEmissions(mixes, series, timeZone, aggregation);
            var utcResult = calculator.CalculateSourceEmissions(mixes, series, TimeZoneInfo.Utc, aggregation);

            Assert.NotNull(result);
            Assert.Single(result.EnergySources);
            Assert.NotNull(utcResult);
            Assert.NotEqual(1, utcResult.EnergySources.Count);
        }

        private static EnergySourceResponse GetExpectedSourceEmissions(Aggregation aggregation, DateTimeOffset dateFrom, DateTimeOffset dateTo) => aggregation switch
        {
            Aggregation.Total or Aggregation.Month or Aggregation.Year => new EnergySourceResponse(new List<EnergySourceDeclaration>
            {
                new(
                    dateFrom.ToUnixTimeSeconds(),
                    dateTo.ToUnixTimeSeconds(),
                    1,
                    new()
                    {
                        { "solar", 0.3m },
                        { "windOnshore", 0.38m },
                        { "bioGas", 0.32m }
                    }
                )
            }),
            Aggregation.Actual or Aggregation.Hour => new EnergySourceResponse(new List<EnergySourceDeclaration>
            {
                new(
                    dateFrom.ToUnixTimeSeconds(),
                    dateFrom.AddHours(1).ToUnixTimeSeconds(),
                    1,
                    new()
                    {
                        { "solar", 0.5m },
                        { "windOnshore", 0.3m },
                        { "bioGas", 0.2m }
                    }
                ),
                new(
                    dateFrom.AddHours(1).ToUnixTimeSeconds(),
                    dateFrom.AddHours(2).ToUnixTimeSeconds(),
                    1,
                    new()
                    {
                        { "solar", 0.4m },
                        { "windOnshore", 0.5m },
                        { "bioGas", 0.1m }
                    }
                ),
                new(
                    dateFrom.AddHours(2).ToUnixTimeSeconds(),
                    dateFrom.AddHours(3).ToUnixTimeSeconds(),
                    1,
                    new()
                    {
                        { "solar", 0.3m },
                        { "windOnshore", 0.3m },
                        { "bioGas", 0.4m }
                    }
                ),
                new(
                    dateFrom.AddHours(3).ToUnixTimeSeconds(),
                    dateFrom.AddHours(4).ToUnixTimeSeconds(),
                    1,
                    new()
                    {
                        { "solar", 0.2m },
                        { "windOnshore", 0.4m },
                        { "bioGas", 0.4m }
                    }
                )
            }),
            Aggregation.Day => new EnergySourceResponse(new List<EnergySourceDeclaration>
            {
                new(
                    dateFrom.ToUnixTimeSeconds(),
                    dateFrom.AddHours(2).ToUnixTimeSeconds(),
                    0.99999m,
                    new()
                    {
                        { "solar", 0.43333m },
                        { "windOnshore", 0.43333m },
                        { "bioGas", 0.13333m }
                    }
                ),
                new(
                    dateFrom.AddHours(2).ToUnixTimeSeconds(),
                    dateFrom.AddHours(4).ToUnixTimeSeconds(),
                    1,
                    new()
                    {
                        { "solar", 0.24286m },
                        { "windOnshore", 0.35714m },
                        { "bioGas", 0.4m }
                    }
                )
            }),
            _ => new EnergySourceResponse(new List<EnergySourceDeclaration>()),
        };
    }
}
