using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;

namespace Tests.Helpers;
public class MeasurementDataSet
{
    public static List<TimeSeries> CreateTimeSeries(string gsrn = "571313121223234323", string gridArea = "DK1", MeterType? type = null) => new()
    {
        new TimeSeries(
            new MeteringPoint(gsrn: gsrn, gridArea: gridArea, type: type ?? MeterType.Consumption),
            CreateMeasurements(gsrn)
        )
    };

    public static List<TimeSeries> CreateHugeValuesTimeSeries(string gsrn = "571313121223234323", string gridArea = "DK1", MeterType? type = null) => new()
    {
        new TimeSeries(
            new MeteringPoint(gsrn: gsrn, gridArea: gridArea, type: type ?? MeterType.Consumption),
            CreateHugeMeasurements(gsrn)
        )
    };

    public static List<TimeSeries> CreateSpecificDateTimeSeries(string gsrn = "571313121223234323", string gridArea = "DK1", MeterType? type = null, DateTimeOffset? startingAt = null, int amount = 100) => new()
    {
        new TimeSeries(
            new MeteringPoint(gsrn: gsrn, gridArea: gridArea, type: type ?? MeterType.Consumption),
            CreateSpecificDateMeasurements(gsrn, startingAt, amount)
        )
    };

    public static List<Measurement> CreateMeasurements(string gsrn = "571313121223234323") => new()
    {
            new Measurement(
                GSRN: gsrn,
                DateFrom: new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 1, 22,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1234,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: gsrn,
                DateFrom:new DateTimeOffset(2021, 1, 1, 23,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo:new DateTimeOffset(2021, 1, 1, 23,59, 59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity:242,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: gsrn,
                DateFrom: new DateTimeOffset(2021, 1, 2, 0,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 2, 0,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 654,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: gsrn,
                DateFrom :new DateTimeOffset(2021, 1, 2, 1,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 2, 1,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1800,
                Quality: Quality.Measured)
    };

    public static List<Measurement> CreateHugeMeasurements(string gsrn = "571313121223234323") => new()
    {
            new Measurement(
                GSRN: gsrn,
                DateFrom: new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 1, 22,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 2000000000L,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: gsrn,
                DateFrom: new DateTimeOffset(2021, 1, 1, 23,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 1, 23,59, 59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1500000000L,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: gsrn,
                DateFrom: new DateTimeOffset(2021, 1, 2, 0,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 2, 0,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1000000000L,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: gsrn,
                DateFrom: new DateTimeOffset(2021, 1, 2, 1,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 2, 1,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1500000000L,
                Quality: Quality.Measured)
    };

    public static List<Measurement> CreateSpecificDateMeasurements(string gsrn = "571313121223234323", DateTimeOffset? startingAt = null, int amount = 100)
    {
        var start = startingAt ?? DateTimeOffset.Now;
        return Enumerable.Range(0, amount).Select(cursor => new Measurement(
                GSRN: gsrn,
                DateFrom: start.AddHours(cursor).ToUnixTimeSeconds(),
                DateTo: start.AddHours(cursor + 1).ToUnixTimeSeconds(),
                Quantity: cursor,
                Quality: Quality.Measured)).ToList();
    }
}
