using API.Models;
using System.Collections.Generic;

namespace Tests;
internal class ProductionAggregationData : MeasurementAggregationDataSetFactory
{
    public List<TimeSeries> CreateTimeSeries()
    {
        return new List<TimeSeries>
    {
        new TimeSeries(
            new MeteringPoint(
                gsrn: "571313121223234323",
                gridArea: "DK1",
                type: MeterType.Production),
            CreateMeasurements()
        )
    };
    }
}

