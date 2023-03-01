using API.Models;

namespace API.Services
{
    public interface IAggregator
    {
        MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, TimeZoneInfo timeZone, Aggregation aggregation);
    }
}
