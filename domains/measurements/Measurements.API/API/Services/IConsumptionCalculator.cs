using API.Models;

namespace API.Services;

public interface IConsumptionCalculator
{
    ConsumptionResponse CalculateConsumption(
        IEnumerable<TimeSeries> measurements,
        long dateFrom,
        long dateTo,
        Aggregation aggregation);
}
