using API.Models;

namespace API.Services;

public interface IConsumptionAggregation
{
    MeasurementResponse CalculateAggregation(
        IEnumerable<TimeSeries> measurements,
        long dateFrom,
        long dateTo,
        Aggregation aggregation);
}
