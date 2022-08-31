using API.Models;

namespace API.Services;

public interface IEmissionsCalculator
{
    public EmissionsResponse CalculateEmission(
        IEnumerable<EmissionRecord> emissionRecords,
        IEnumerable<TimeSeries> measurements,
        Aggregation aggregation);
}
