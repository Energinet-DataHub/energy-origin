using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Helpers;
using API.Models;
using API.Services;
using Xunit;

namespace Tests
{
    public sealed class CalculateSourceEmissionShareTest
    {
        readonly SourceEmissionShareDataSetFactory sourceEmissionShareDataSetFactory = new();

        [Fact]
        public void EmissionSharesAndMeasurements_CalculateTotalEmission_TotalAnRelativeEmission()
        {
            // Arrange
            var dateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
            var dateTo = new DateTime(2021, 1, 2, 1, 59, 59, DateTimeKind.Utc);
            var timeSeries = sourceEmissionShareDataSetFactory.CreateTimeSeries();
            var emissionShares = sourceEmissionShareDataSetFactory.CreateEmissionsShares();
            Environment.SetEnvironmentVariable("RENEWABLESOURCES",
                "Wood, Waste, Straw , BioGas, Solar, WindInshore, WindOfShore");


            var sut = new SourcesCalculator();

            // Act
            var result = sut.CalculateSourceEmissions(timeSeries, emissionShares, dateFrom.ToUnixTime(),
                dateTo.ToUnixTime(), Aggregation.Hour);
        }
    }
}
