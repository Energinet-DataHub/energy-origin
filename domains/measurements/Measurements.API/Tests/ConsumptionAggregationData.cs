using System.Collections.Generic;
using API.Models;

namespace Tests;

internal class ConsumptionAggregationData : MeasurementAggregationDataSetFactory
{
    public static List<TimeSeries> CreateTimeSeries() => new()
    {
        new TimeSeries(
            new MeteringPoint(
                gsrn: "571313121223234323",
                gridArea: "DK1",
                type: MeterType.Consumption),
            CreateMeasurements()
        )
    };

    public static List<TimeSeries> CreateTimeSeriesHugeValues() => new()
    {
        new TimeSeries(
            new MeteringPoint(
                gsrn: "571313121223234323",
                gridArea: "DK1",
                type: MeterType.Consumption),
            CreateMeasurementHuge()
        )
    };
}
