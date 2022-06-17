﻿using System;
using System.Collections.Generic;
using System.Linq;
using API.Helpers;
using API.Models;
using API.Services;
using Xunit;
using EnergyOriginDateTimeExtension;

namespace Tests
{
    public sealed class CalculateSourceEmissionShareTest
    {
        readonly SourceEmissionShareDataSetFactory dataSetFactory = new();

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
            var timeSeries = dataSetFactory.CreateTimeSeries();
            var emissionShares = dataSetFactory.CreateEmissionsShares();
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

        EnergySourceResponse GetExpectedSourceEmissions(Aggregation aggregation, DateTime dateFrom, DateTime dateTo)
        {
            switch (aggregation)
            {
                case Aggregation.Total:
                case Aggregation.Month:
                case Aggregation.Year:
                    return new EnergySourceResponse(

                        new List<EnergySourceDeclaration>
                        {
                            new(
                                dateFrom.ToUnixTime(),
                                dateTo.ToUnixTime(),
                                1,
                                new()
                                {
                                    { "solar", 0.3f },
                                    { "windOnshore", 0.38f },
                                    { "bioGas", 0.32f }
                                }
                            )
                        });
                case Aggregation.Actual:
                case Aggregation.Hour:
                    return new EnergySourceResponse(

                        new List<EnergySourceDeclaration>
                        {
                            new(
                                dateFrom.ToUnixTime(),
                                dateFrom.AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                1,
                                new()
                                {
                                    { "solar", 0.5f },
                                    { "windOnshore", 0.3f },
                                    { "bioGas", 0.2f }
                                }
                            ),
                            new(
                                dateFrom.AddHours(1).ToUnixTime(),
                                dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                1,
                                new()
                                {
                                    { "solar", 0.4f },
                                    { "windOnshore", 0.5f },
                                    { "bioGas", 0.1f }
                                }
                            ),
                            new(
                                dateFrom.AddHours(2).ToUnixTime(),
                                dateFrom.AddHours(2).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                1,
                                new()
                                {
                                    { "solar", 0.3f },
                                    { "windOnshore", 0.3f },
                                    { "bioGas", 0.4f }
                                }
                            ),
                            new(
                                dateFrom.AddHours(3).ToUnixTime(),
                                dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                1,
                                new()
                                {
                                    { "solar", 0.2f },
                                    { "windOnshore", 0.4f },
                                    { "bioGas", 0.4f }
                                }
                            )
                        });
                case Aggregation.Day:
                    return new EnergySourceResponse(

                        new List<EnergySourceDeclaration>
                        {
                            new(
                                dateFrom.ToUnixTime(),
                                dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                0.99999f,
                                new()
                                {
                                    { "solar", 0.43333f },
                                    { "windOnshore", 0.43333f },
                                    { "bioGas", 0.13333f }
                                }
                            ),
                            new(
                                dateFrom.AddHours(2).ToUnixTime(),
                                dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                1,
                                new()
                                {
                                    { "solar", 0.24286f },
                                    { "windOnshore", 0.35714f },
                                    { "bioGas", 0.4f }
                                }
                            )
                        });


                default:
                    return new EnergySourceResponse(new List<EnergySourceDeclaration>());
            }
        }
    }
}