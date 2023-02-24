using System;
using System.Collections.Generic;
using API.Models;
using API.Models.EnergiDataService;
using EnergyOriginDateTimeExtension;

namespace Tests;

internal class CalculateEmissionDataSetFactory
{
    public static List<TimeSeries> CreateTimeSeries() => new()
    {
            new TimeSeries
            (
                new MeteringPoint("571313121223234323", "DK1", MeterType.Consumption),
                CreateMeasurementsFirstMP()
            ),
            new TimeSeries
            (
                new MeteringPoint("571313121223234324", "DK1", MeterType.Consumption),
                CreateMeasurementsSecondMP()
            )
        };

    public static List<TimeSeries> CreateTimeSeriesHugeValues() => new()
    {
        new TimeSeries
        (
            new MeteringPoint("571313121223234323", "DK1", MeterType.Consumption),
            CreateMeasurementHugeNumbers()
        )
    };


    public static List<TimeSeries> CreateTimeSeriesForMismatchMeasurements() => new()
    {
        new TimeSeries
        (
            new MeteringPoint("571313121223234323", "DK1", MeterType.Consumption),
            CreateMeasurementsFirstMP()
        ),
        new TimeSeries
        (
            new MeteringPoint("571313121223234324", "DK1", MeterType.Consumption),
            CreateMeasurementsForMismatch()
        )
    };

    public static List<Measurement> CreateMeasurementsFirstMP() => new()
    {
        new Measurement(
            GSRN: "571313121223234323",
            DateFrom: new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1234,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234323",
            DateFrom: new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 0,0, 0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 242,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234323",
            DateFrom: new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 654,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234323",
            DateFrom: new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1800,
            Quality: Quality.Measured
            ),
    };
    public static List<Measurement> CreateMeasurementsSecondMP() => new()
    {
        new Measurement(
            GSRN: "571313121223234324",
            DateFrom: new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 789,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234324",
            DateFrom: new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 0,0, 0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1212,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234324",
            DateFrom: new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 324,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234324",
            DateFrom: new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1233,
            Quality: Quality.Measured
            )
    };

    public static List<Measurement> CreateMeasurementsForMismatch() => new()
    {
        new Measurement(
            GSRN: "571313121223234324",
            DateFrom: new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 789,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234324",
            DateFrom: new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 0,0, 0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1212,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234324",
            DateFrom: new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 324,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234324",
            DateFrom: new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1233,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234324",
            DateFrom: new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 3,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 2140,
            Quality: Quality.Measured
            )
    };

    public static List<EmissionRecord> CreateEmissions() => new()
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

    public static List<Measurement> CreateMeasurementHugeNumbers() => new()
    {
        new Measurement(
            GSRN: "571313121223234323",
            DateFrom: new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 1, 22,59,59, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 2000000000L,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234323",
            DateFrom: new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 1, 23,59, 59, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1500000000L,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234323",
            DateFrom: new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 0,59,59, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1000000000L,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: "571313121223234323",
            DateFrom: new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 1,59,59, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1500000000L,
            Quality: Quality.Measured
            )
    };
}
