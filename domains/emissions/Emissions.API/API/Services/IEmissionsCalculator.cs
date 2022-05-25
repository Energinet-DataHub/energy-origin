using API.Models;

namespace API.Services;

public interface IEmissionsCalculator
{
    public IEnumerable<Emissions> CalculateEmission(List<EmissionRecord> emissionRecords, IEnumerable<TimeSeries> measurements,
        long dateFrom, long dateTo);
}
