using System;
using System.Collections.Generic;
using API.Models;

namespace Tests.Helpers;
public class MeasurementDataSet
{
    public static List<TimeSeries> CreateTimeSeries(string gsrn = "571313121223234323", string gridArea = "DK1", MeterType? type = null) => new()
    {
        new TimeSeries(
            new MeteringPoint(gsrn: gsrn, gridArea: gridArea, type: type ?? MeterType.Consumption),
            CreateMeasurements()
        )
    };

    public static List<TimeSeries> CreateTimeSeriesHugeValues(string gsrn = "571313121223234323", string gridArea = "DK1", MeterType? type = null) => new()
    {
        new TimeSeries(
            new MeteringPoint(gsrn: gsrn, gridArea: gridArea, type: type ?? MeterType.Consumption),
            CreateMeasurementHuge()
        )
    };

    public static List<Measurement> CreateMeasurements() => new()
    {
            new Measurement(
                GSRN: "571313121223234323",
                DateFrom: new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 1, 22,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1234,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: "571313121223234323",
                DateFrom:new DateTimeOffset(2021, 1, 1, 23,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo:new DateTimeOffset(2021, 1, 1, 23,59, 59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity:242,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: "571313121223234323",
                DateFrom: new DateTimeOffset(2021, 1, 2, 0,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 2, 0,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 654,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: "571313121223234323",
                DateFrom :new DateTimeOffset(2021, 1, 2, 1,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 2, 1,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1800,
                Quality: Quality.Measured)
        };

    public static List<Measurement> CreateMeasurementHuge() => new()
    {
            new Measurement(
                GSRN: "571313121223234323",
                DateFrom: new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 1, 22,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 2000000000L,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: "571313121223234323",
                DateFrom: new DateTimeOffset(2021, 1, 1, 23,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 1, 23,59, 59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1500000000L,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: "571313121223234323",
                DateFrom: new DateTimeOffset(2021, 1, 2, 0,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 2, 0,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1000000000L,
                Quality: Quality.Measured),
            new Measurement(
                GSRN: "571313121223234323",
                DateFrom: new DateTimeOffset(2021, 1, 2, 1,0,0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DateTo: new DateTimeOffset(2021, 1, 2, 1,59,59, TimeSpan.Zero).ToUnixTimeSeconds(),
                Quantity: 1500000000L,
                Quality: Quality.Measured)
        };
}
