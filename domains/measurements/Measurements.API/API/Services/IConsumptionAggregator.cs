using API.Models;

namespace API.Services;

public interface IConsumptionAggregator
{
    MeasurementResponse CalculateAggregation(
        IEnumerable<TimeSeries> measurements,
        long dateFrom,
        long dateTo,
        Aggregation aggregation);
}
