using API.Models;

namespace API.Services;

public interface IConsumptionsCalculator
{
    public ConsumptionsResponse CalculateConsumptions(IEnumerable<TimeSeries> measurements,
        long dateFrom, long dateTo, Aggregation aggregation);
}
