using System;
using System.Collections.Generic;
using API.Models;

namespace Tests;

internal class CalculateEmissionDataSetFactory
{
    public List<Measurement> CreateMeasurements()
    {
        return new List<Measurement>
        {
            new Measurement(
                "571313121223234323",
                new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc),
                new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc),
                1234,
                Quality.Measured),
            new Measurement(
                "571313121223234323",
                new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc),
                new DateTime(2021, 1, 2, 0,0, 0, DateTimeKind.Utc),
                242,
                Quality.Measured),
            new Measurement(
                "571313121223234323",
                new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc),
                new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc),
                654,
                Quality.Measured),
            new Measurement(
                "571313121223234323",
                new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc),
                new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc),
                1800,
                Quality.Measured)
        };
    }

    public List<TimeSeries> CreateTimeSeries()
    {
        return new List<TimeSeries>
        {
            new TimeSeries
            (
                new MeteringPoint("571313121223234323", "DK1", MeterType.Consumption),
                CreateMeasurements()
            ),
            new TimeSeries
            (
                new MeteringPoint("571313121223234324", "DK1", MeterType.Consumption),
                new List<Measurement>
                {
                    new Measurement(
                        "571313121223234324",
                        new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc),
                        new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc),
                        789,
                        Quality.Measured),
                    new Measurement(
                        "571313121223234324",
                        new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc),
                        new DateTime(2021, 1, 2, 0,0, 0, DateTimeKind.Utc),
                        1212,
                        Quality.Measured),
                    new Measurement(
                        "571313121223234324",
                        new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc),
                        new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc),
                        324,
                        Quality.Measured),
                    new Measurement(
                        "571313121223234324",
                        new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc),
                        new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc),
                        1233,
                        Quality.Measured)
                }
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