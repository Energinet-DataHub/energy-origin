using System;
using System.Collections.Generic;
using API.Helpers;
using API.Models;

namespace Tests
{
    internal class SourceEmissionShareDataSetFactory
    {
        public List<Measurement> CreateMeasurements()
        {
            return new List<Measurement>
            {
                new Measurement
                {
                    DateFrom = new DateTime(2021, 1, 1, 22,0,0, DateTimeKind.Utc).ToUnixTime(),
                    DateTo = new DateTime(2021, 1, 1, 22,59,59, DateTimeKind.Utc).ToUnixTime(),
                    Quantity = 1000
                },
                new Measurement
                {
                    DateFrom = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                    DateTo = new DateTime(2021, 1, 1, 23,59, 59, DateTimeKind.Utc).ToUnixTime(),
                    Quantity = 2000
                },
                new Measurement
                {
                    DateFrom = new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                    DateTo = new DateTime(2021, 1, 2, 0,59,59, DateTimeKind.Utc).ToUnixTime(),
                    Quantity = 3000
                },
                new Measurement
                {
                    DateFrom = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                    DateTo = new DateTime(2021, 1, 2, 1,59,59, DateTimeKind.Utc).ToUnixTime(),
                    Quantity = 4000
                }
            };
        }

        public List<TimeSeries> CreateTimeSeries()
        {
            return new List<TimeSeries>
            {
                new TimeSeries
                (
                    new MeteringPoint(571313121223234323, "DK1"),
                    CreateMeasurements()
                )
            };
        }

        public DeclarationProduction CreateEmissionsShares()
        {
            return new DeclarationProduction(String.Empty, true, 
                new Result(new List<Record>
                {
                    new Record(50, new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc), "Final", "DK1", "Solar"),
                    new Record(30, new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc), "Final", "DK1", "Wind"),
                    new Record(20, new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc), "Final", "DK1", "Bio"),

                    new Record(40, new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc), "Final", "DK1", "Solar"),
                    new Record(50, new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc), "Final", "DK1", "Wind"),
                    new Record(10, new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc), "Final", "DK1", "Bio"),

                    new Record(30, new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc), "Final", "DK1", "Solar"),
                    new Record(30, new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc), "Final", "DK1", "Wind"),
                    new Record(40, new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc), "Final", "DK1", "Bio"),

                    new Record(20, new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc), "Final", "DK1", "Solar"),
                    new Record(40, new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc), "Final", "DK1", "Wind"),
                    new Record(40, new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc), "Final", "DK1", "Bio"),

                }, new List<Field>(), String.Empty));
        }
    }
}
