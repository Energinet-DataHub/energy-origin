using API.Models;

namespace API.Services
{
    public interface ISourcesCalculator
    {
        EnergySourceResponse CalculateSourceEmissions(IEnumerable<MixRecord> records, IEnumerable<TimeSeries> measurements, TimeZoneInfo timeZone, Aggregation aggregation);
    }
}
