using System.Collections.Generic;
using API.Models;

namespace Tests;

internal class ConsumptionAggregationData : MeasurementAggregationDataSetFactory
{
    public List<TimeSeries> CreateTimeSeries()
    {
        return new List<TimeSeries>
        {
            new TimeSeries(
                new MeteringPoint(
                    gsrn: "571313121223234323",
                    gridArea: "DK1",
                    type: MeterType.Consumption),
                CreateMeasurements()
            )
        };
    }

    public List<TimeSeries> CreateTimeSeriesHugeValues()
    {
        return new List<TimeSeries>
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
}
