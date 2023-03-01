using API.Models;
using API.Models.EnergiDataService;

namespace API.Services;

public interface IEmissionsCalculator
{
    public EmissionsResponse CalculateEmission(
        IEnumerable<EmissionRecord> emissionRecords,
        IEnumerable<TimeSeries> measurements,
        TimeZoneInfo timeZone,
        Aggregation aggregation);
}
