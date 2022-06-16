using System;
using System.Collections.Generic;
using API.Helpers;
using API.Models;
using EnergyOriginDateTimeExtension;

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
}
