using API.Models;

namespace API.Services
{
    public interface IProductionAggregator
    {
        MeasurementResponse CalculateAggregation(
        IEnumerable<TimeSeries> measurements,
        long dateFrom,
        long dateTo,
        Aggregation aggregation);
    }
}
