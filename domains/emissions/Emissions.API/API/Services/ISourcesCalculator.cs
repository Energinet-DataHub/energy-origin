using API.Models;

namespace API.Services
{
    public interface ISourcesCalculator
    {
        EnergySourceResponse CalculateSourceEmissions(IEnumerable<TimeSeries> measurements, IEnumerable<MixRecord> records, Aggregation aggregation);
    }
}
