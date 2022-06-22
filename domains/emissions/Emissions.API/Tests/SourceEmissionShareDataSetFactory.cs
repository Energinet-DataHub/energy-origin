using System;
using System.Collections.Generic;
using API.Helpers;
using API.Models;
using EnergyOriginDateTimeExtension;

namespace Tests
{
    internal class SourceEmissionShareDataSetFactory
    {
        public List<Measurement> CreateMeasurements()
        {
            return new List<Measurement>
            {
                new Measurement(
                    "571313121223234323",
                    new DateTime(2021, 1, 1, 22,0,0, DateTimeKind.Utc).ToUnixTime(),
                    new DateTime(2021, 1, 1, 22,59,59, DateTimeKind.Utc).ToUnixTime(),
                    1000,
                    Quality.Measured),
                new Measurement(
                    "571313121223234323",
                    new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                    new DateTime(2021, 1, 1, 23,59, 59, DateTimeKind.Utc).ToUnixTime(),
                    2000,
                    Quality.Measured),
                new Measurement(
                    "571313121223234323",
                    new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                    new DateTime(2021, 1, 2, 0,59,59, DateTimeKind.Utc).ToUnixTime(),
                    3000,
                    Quality.Measured),
                new Measurement(
                    "571313121223234323",
                    new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                    new DateTime(2021, 1, 2, 1,59,59, DateTimeKind.Utc).ToUnixTime(),
                    4000,
                    Quality.Measured)
            };
        }

        public List<TimeSeries> CreateTimeSeries()
        {
            return new List<TimeSeries>
            {
                new TimeSeries
                (
                    new MeteringPoint("571313121223234323", "DK1", MeterType.consumption),
                    CreateMeasurements()
                )
            };
        }

        public List<MixRecord> CreateEmissionsShares()
        {
            return new List<MixRecord>
                {
                    new MixRecord(50, new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc), "Final", "DK1", "Solar"),
                    new MixRecord(30, new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc), "Final", "DK1", "WindOnshore"),
                    new MixRecord(20, new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc), "Final", "DK1", "BioGas"),

                    new MixRecord(40, new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc), "Final", "DK1", "Solar"),
                    new MixRecord(50, new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc), "Final", "DK1", "WindOnshore"),
                    new MixRecord(10, new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc), "Final", "DK1", "BioGas"),

                    new MixRecord(30, new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc), "Final", "DK1", "Solar"),
                    new MixRecord(30, new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc), "Final", "DK1", "WindOnshore"),
                    new MixRecord(40, new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc), "Final", "DK1", "BioGas"),

                    new MixRecord(20, new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc), "Final", "DK1", "Solar"),
                    new MixRecord(40, new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc), "Final", "DK1", "WindOnshore"),
                    new MixRecord(40, new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc), "Final", "DK1", "BioGas"),

            };
        }
    }
}
