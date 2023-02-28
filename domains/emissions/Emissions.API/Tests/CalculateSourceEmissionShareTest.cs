using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using API.Services;
using Xunit;

namespace Tests
{
    public sealed class CalculateSourceEmissionShareTest
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
            var dateFrom = new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero);
            var dateTo = new DateTimeOffset(2021, 1, 2, 1, 59, 59, TimeSpan.Zero);
            var timeSeries = SourceEmissionShareDataSetFactory.CreateTimeSeries();
            var emissionShares = SourceEmissionShareDataSetFactory.CreateEmissionsShares();
            Environment.SetEnvironmentVariable("RENEWABLESOURCES", "wood,waste,straw,bioGas,solar,windOnshore,windOffshore");
            Environment.SetEnvironmentVariable("WASTERENEWABLESHARE", "55");

            var calculator = new SourcesCalculator();

            var result = calculator.CalculateSourceEmissions(timeSeries, emissionShares, TimeZoneInfo.Utc, aggregation);

            Assert.NotNull(result);
            var expected = GetExpectedSourceEmissions(aggregation, dateFrom, dateTo).EnergySources;
            Assert.Equal(expected.Select(x => x.Renewable), result.EnergySources.Select(x => x.Renewable));
            Assert.Equal(expected.Select(x => x.Ratios), result.EnergySources.Select(x => x.Ratios));
            Assert.Equal(expected.Select(x => x.DateFrom), result.EnergySources.Select(x => x.DateFrom));
            Assert.Equal(expected.Select(x => x.DateTo), result.EnergySources.Select(x => x.DateTo));
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
                    dateFrom.AddMinutes(59).AddSeconds(59).ToUnixTimeSeconds(),
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
                    dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTimeSeconds(),
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
                    dateFrom.AddHours(2).AddMinutes(59).AddSeconds(59).ToUnixTimeSeconds(),
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
                    dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTimeSeconds(),
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
                    dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTimeSeconds(),
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
                    dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTimeSeconds(),
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
