using System;
using System.Collections.Generic;
using API.Helpers;
using API.Models;


namespace Tests;

internal class CalculateConsumptionDataSetFactory
{
    public List<Measurement> CreateMeasurements()
    {
        return new List<Measurement>
        {
            new Measurement
            {
                DateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 22,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1234
            },
            new Measurement
            {
                DateFrom = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 23,59, 59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 242
            },
            new Measurement
            {
                DateFrom = new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 0,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 654
            },
            new Measurement
            {
                DateFrom = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 1,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1800
            }
        };
    }

    public List<TimeSeries> CreateTimeSeries()
    {
        return new List<TimeSeries>
        {
            new TimeSeries
            (
                new MeteringPoint("571313121223234323"),
                CreateMeasurements()
            )
        };
    }

    public IEnumerable<Consumption> GetExpectedConsumption(Aggregation aggregation, DateTime dateFrom, DateTime dateTo)
    {
        return aggregation switch
        {
            Aggregation.Actual or Aggregation.Hour => new List<Consumption>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        1234f
                    ),
                    new(
                        dateFrom.AddHours(1).ToUnixTime(),
                        dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        242f
                    ),
                    new(
                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(2).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        654f
                    ),
                    new(
                        dateFrom.AddHours(3).ToUnixTime(),
                        dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        1800f
                    ),
                },
            Aggregation.Day => new List<Consumption>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        1476f
                    ),
                    new(

                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        2454f
                    )
                },
            Aggregation.Month or Aggregation.Year or Aggregation.Total => new List<Consumption>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateTo.ToUnixTime(),
                        3930f
                    )
                },
            _ => new List<Consumption>(),
        };
    }
}
