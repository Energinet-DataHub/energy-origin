using System;
using System.Collections.Generic;
using API.Helpers;
using API.Models;

namespace Tests;

public class DateSetFactory
{
    public List<Measurement> CreateMeasurements()
    {
        return new List<Measurement>
        {
            new Measurement
            {
                DateFrom = new DateTime(2021, 1, 1, 22, 0, 0).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 22,59,59).ToUnixTime(),
                Quantity = 1234
            },
            new Measurement
            {
                DateFrom = new DateTime(2021, 1, 1, 23,0,0).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 23,59,59).ToUnixTime(),
                Quantity = 242
            },
            new Measurement
            {
                DateFrom = new DateTime(2021, 1, 2, 0,0,0).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 0,59,59).ToUnixTime(),
                Quantity = 654
            },
            new Measurement
            {
                DateFrom = new DateTime(2021, 1, 2, 1,0,0).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 1,59,59).ToUnixTime(),
                Quantity = 1800
            }
        };
    }

    public List<TimeSeries> CreateTimeSeries()
    {
        return new List<TimeSeries>
        {
            new TimeSeries
            {
                MeteringPoint = new MeteringPoint {Gsrn = 571313121223234323, GridArea = "DK1"},
                Measurements = CreateMeasurements()
            }
        };
    }

    public List<EmissionRecord> CreateEmissions()
    {
        return new List<EmissionRecord>
        {
            new EmissionRecord
            {
                CO2PerkWh = 124,
                GridArea = "DK1",
                HourUTC = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc)
            },
            new EmissionRecord
            {
                CO2PerkWh = 234,
                GridArea = "DK1",
                HourUTC = new DateTime(2021, 1, 1, 23, 0, 0, DateTimeKind.Utc)
            },
            new EmissionRecord
            {
                CO2PerkWh = 85,
                GridArea = "DK1",
                HourUTC = new DateTime(2021, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new EmissionRecord
            {
                CO2PerkWh = 120,
                GridArea = "DK1",
                HourUTC = new DateTime(2021, 1, 2, 1, 0, 0, DateTimeKind.Utc)
            },
        };
    }
}