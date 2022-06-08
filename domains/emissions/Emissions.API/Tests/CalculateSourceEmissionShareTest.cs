using System;
using System.Collections.Generic;
using System.Linq;
using API.Helpers;
using API.Models;
using API.Services;
using Xunit;

namespace Tests
{
    public sealed class CalculateSourceEmissionShareTest
    {
        readonly SourceEmissionShareDataSetFactory sourceEmissionShareDataSetFactory = new();

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
            var timeSeries = sourceEmissionShareDataSetFactory.CreateTimeSeries();
            var emissionShares = sourceEmissionShareDataSetFactory.CreateEmissionsShares();
            Environment.SetEnvironmentVariable("RENEWABLESOURCES",
                "Wood,Waste,Straw,BioGas,Solar,WindOnshore,WindOffshore");


            var sut = new SourcesCalculator();

            // Act
            var result = sut.CalculateSourceEmissions(timeSeries, emissionShares, aggregation);

            //Assert
            Assert.NotNull(result);
            var emissionsEnumerable = GetExpectedSourceEmissions(aggregation, dateFrom, dateTo).EnergySources;
            Assert.Equal(emissionsEnumerable.Select(_ => _.Renewable), result.EnergySources.Select(_ => _.Renewable));
            Assert.Equal(emissionsEnumerable.Select(_ => _.Ratios), result.EnergySources.Select(_ => _.Ratios));
            Assert.Equal(emissionsEnumerable.Select(_ => _.DateFrom), result.EnergySources.Select(_ => _.DateFrom));
            Assert.Equal(emissionsEnumerable.Select(_ => _.DateTo), result.EnergySources.Select(_ => _.DateTo));
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
                                    { "Solar", 0.3f },
                                    { "WindOnshore", 0.38f },
                                    { "BioGas", 0.32f }
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
                                    { "Solar", 0.5f },
                                    { "WindOnshore", 0.3f },
                                    { "BioGas", 0.2f }
                                }
                            ),
                            new(
                                dateFrom.AddHours(1).ToUnixTime(),
                                dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                1,
                                new()
                                {
                                    { "Solar", 0.4f },
                                    { "WindOnshore", 0.5f },
                                    { "BioGas", 0.1f }
                                }
                            ),
                            new(
                                dateFrom.AddHours(2).ToUnixTime(),
                                dateFrom.AddHours(2).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                1,
                                new()
                                {
                                    { "Solar", 0.3f },
                                    { "WindOnshore", 0.3f },
                                    { "BioGas", 0.4f }
                                }
                            ),
                            new(
                                dateFrom.AddHours(3).ToUnixTime(),
                                dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                1,
                                new()
                                {
                                    { "Solar", 0.2f },
                                    { "WindOnshore", 0.4f },
                                    { "BioGas", 0.4f }
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
                                0.9999f,
                                new()
                                {
                                    { "Solar", 0.4333f },
                                    { "WindOnshore", 0.4333f },
                                    { "BioGas", 0.1333f }
                                }
                            ),
                            new(
                                dateFrom.AddHours(2).ToUnixTime(),
                                dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                                1,
                                new()
                                {
                                    { "Solar", 0.2429f },
                                    { "WindOnshore", 0.3571f },
                                    { "BioGas", 0.4f }
                                }
                            )
                        });


                default:
                    return new EnergySourceResponse(new List<EnergySourceDeclaration>());
            }
        }
    }
}