using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using API.Services;
using EnergyOriginDateTimeExtension;
using Xunit;

namespace Tests
{
    public sealed class CalculateSourceEmissionShareTest
    {
        private readonly SourceEmissionShareDataSetFactory dataSetFactory = new();

        [Theory]
        [InlineData(Aggregation.Total)]
        [InlineData(Aggregation.Actual)]
        [InlineData(Aggregation.Hour)]
        [InlineData(Aggregation.Day)]
        [InlineData(Aggregation.Month)]
        [InlineData(Aggregation.Year)]
        public void EmissionSharesAndMeasurements_CalculateTotalEmission_TotalAnRelativeEmission(Aggregation aggregation)
        {
            // Arrange
            var dateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
            var dateTo = new DateTime(2021, 1, 2, 1, 59, 59, DateTimeKind.Utc);
            var timeSeries = SourceEmissionShareDataSetFactory.CreateTimeSeries();
            var emissionShares = SourceEmissionShareDataSetFactory.CreateEmissionsShares();
            Environment.SetEnvironmentVariable("RENEWABLESOURCES", "wood,waste,straw,bioGas,solar,windOnshore,windOffshore");
            Environment.SetEnvironmentVariable("WASTERENEWABLESHARE", "55");

            var calculator = new SourcesCalculator();

            // Act
            var result = calculator.CalculateSourceEmissions(timeSeries, emissionShares, aggregation);

            //Assert
            Assert.NotNull(result);
            var expected = GetExpectedSourceEmissions(aggregation, dateFrom, dateTo).EnergySources;
            Assert.Equal(expected.Select(_ => _.Renewable), result.EnergySources.Select(_ => _.Renewable));
            Assert.Equal(expected.Select(_ => _.Ratios), result.EnergySources.Select(_ => _.Ratios));
            Assert.Equal(expected.Select(_ => _.DateFrom), result.EnergySources.Select(_ => _.DateFrom));
            Assert.Equal(expected.Select(_ => _.DateTo), result.EnergySources.Select(_ => _.DateTo));
        }

        private static EnergySourceResponse GetExpectedSourceEmissions(Aggregation aggregation, DateTime dateFrom, DateTime dateTo) => aggregation switch
        {
            Aggregation.Total or Aggregation.Month or Aggregation.Year => new EnergySourceResponse(new List<EnergySourceDeclaration>
            {
                new(
                    dateFrom.ToUnixTime(),
                    dateTo.ToUnixTime(),
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
                    dateFrom.ToUnixTime(),
                    dateFrom.AddMinutes(59).AddSeconds(59).ToUnixTime(),
                    1,
                    new()
                    {
                        { "solar", 0.5m },
                        { "windOnshore", 0.3m },
                        { "bioGas", 0.2m }
                    }
                ),
                new(
                    dateFrom.AddHours(1).ToUnixTime(),
                    dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                    1,
                    new()
                    {
                        { "solar", 0.4m },
                        { "windOnshore", 0.5m },
                        { "bioGas", 0.1m }
                    }
                ),
                new(
                    dateFrom.AddHours(2).ToUnixTime(),
                    dateFrom.AddHours(2).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                    1,
                    new()
                    {
                        { "solar", 0.3m },
                        { "windOnshore", 0.3m },
                        { "bioGas", 0.4m }
                    }
                ),
                new(
                    dateFrom.AddHours(3).ToUnixTime(),
                    dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
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
                    dateFrom.ToUnixTime(),
                    dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                    0.99999m,
                    new()
                    {
                        { "solar", 0.43333m },
                        { "windOnshore", 0.43333m },
                        { "bioGas", 0.13333m }
                    }
                ),
                new(
                    dateFrom.AddHours(2).ToUnixTime(),
                    dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
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
