using API.Models;
using EnergyOriginDateTimeExtension;
using System;
using System.Collections.Generic;

namespace Tests;

internal class ConsumptionAggregationDataSetFactory
{
    public List<Measurement> CreateMeasurements()
    {
        return new List<Measurement>
        {
            new Measurement
            (
                gsrn: "571313121223234323",
                dateFrom: new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
                dateTo: new DateTime(2021, 1, 1, 22,59,59, DateTimeKind.Utc).ToUnixTime(),
                quantity: 1234,
                quality: EnergyQuantityValueQuality.measured
            ),
            new Measurement
            (
                gsrn: "571313121223234323",
                dateFrom:new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                dateTo:new DateTime(2021, 1, 1, 23,59, 59, DateTimeKind.Utc).ToUnixTime(),
                quantity:242,
                quality: EnergyQuantityValueQuality.measured
            ),
            new Measurement
            (
                gsrn: "571313121223234323",
                dateFrom: new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                dateTo: new DateTime(2021, 1, 2, 0,59,59, DateTimeKind.Utc).ToUnixTime(),
                quantity: 654,
                quality: EnergyQuantityValueQuality.measured
            ),
            new Measurement
            (
                gsrn: "571313121223234323",
                dateFrom :new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                dateTo: new DateTime(2021, 1, 2, 1,59,59, DateTimeKind.Utc).ToUnixTime(),
                quantity: 1800,
                quality: EnergyQuantityValueQuality.measured
            )
        };
    }

    public List<TimeSeries> CreateTimeSeries()
    {
        return new List<TimeSeries>
        {
            new TimeSeries
            (
                new MeteringPoint
                (
                    gsrn: "571313121223234323",
                    gridArea: "DK1",
                    type: MeterType.consumption
                ),
                CreateMeasurements()
            )
        };
    }
}
