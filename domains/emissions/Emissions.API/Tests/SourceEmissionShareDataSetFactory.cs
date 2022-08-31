using System;
using System.Collections.Generic;
using API.Models;
using EnergyOriginDateTimeExtension;

namespace Tests;

internal static class SourceEmissionShareDataSetFactory
{
    public static List<Measurement> CreateMeasurements(string MP) => new()
    {
        new Measurement(
            GSRN: MP,
            DateFrom: new DateTime(2021, 1, 1, 22,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 1000,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: MP,
            DateFrom: new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 0,0, 0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 2000,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: MP,
            DateFrom: new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 3000,
            Quality: Quality.Measured
            ),
        new Measurement(
            GSRN: MP,
            DateFrom: new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
            DateTo: new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc).ToUnixTime(),
            Quantity: 4000,
            Quality: Quality.Measured
        )
    };

    public static List<TimeSeries> CreateTimeSeries() => new()
    {
        new TimeSeries
        (
            new MeteringPoint("571313121223234323", "DK1", MeterType.Consumption),
            CreateMeasurements("571313121223234323")
        ),
        new TimeSeries
        (
            new MeteringPoint("571313121223234341", "DK1", MeterType.Consumption),
            CreateMeasurements("571313121223234341")
        )
    };

    public static List<TimeSeries> CreateEmptyTimeSeries => new();

    public static List<MixRecord> CreateEmissionsShares() => new()
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
