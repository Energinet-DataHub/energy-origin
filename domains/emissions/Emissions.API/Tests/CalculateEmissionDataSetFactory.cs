using System;
using System.Collections.Generic;
using API.Helpers;
using API.Models;

namespace Tests;

internal class CalculateEmissionDataSetFactory
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
                new MeteringPoint("571313121223234323", "DK1"),
                CreateMeasurements()
            )
        };
    }

    public List<EmissionRecord> CreateEmissions()
    {
        return new List<EmissionRecord>
        {
            new EmissionRecord
            (
                gridArea: "DK1",
                nOXPerkWh: 0,
                cO2PerkWh: 124,
                hourUTC: new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc)
            ),
            new EmissionRecord
            (
                gridArea: "DK1",
                nOXPerkWh: 0,
                cO2PerkWh: 234,
                hourUTC: new DateTime(2021, 1, 1, 23, 0, 0, DateTimeKind.Utc)
            ),
            new EmissionRecord
            (
                gridArea: "DK1",
                nOXPerkWh: 0,
                cO2PerkWh: 85,
                hourUTC: new DateTime(2021, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            ),
            new EmissionRecord
            (
                gridArea: "DK1",
                nOXPerkWh: 0,
                cO2PerkWh: 120,
                hourUTC: new DateTime(2021, 1, 2, 1, 0, 0, DateTimeKind.Utc)
            ),
        };
    }
}
