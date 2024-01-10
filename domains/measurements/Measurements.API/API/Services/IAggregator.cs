using API.Models;
using API.OldModels;
using API.OldModels.Response;

namespace API.Services
{
    public interface IAggregator
    {
        MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, TimeZoneInfo timeZone, Aggregation aggregation);
    }
}
