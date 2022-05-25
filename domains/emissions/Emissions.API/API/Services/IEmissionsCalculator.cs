using API.Models;

namespace API.Services;

public interface IEmissionsCalculator
{
    public Emissions CalculateTotalEmission(List<EmissionRecord> emissionRecords, IEnumerable<TimeSeries> measurements,
        long dateFrom, long dateTo);
}
