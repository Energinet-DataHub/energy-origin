using System;
using System.Collections.Generic;
using API.Models;
using API.Models.Response;

namespace API.Services
{
    public interface IAggregator
    {
        MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, TimeZoneInfo timeZone, Aggregation aggregation);
    }
}
