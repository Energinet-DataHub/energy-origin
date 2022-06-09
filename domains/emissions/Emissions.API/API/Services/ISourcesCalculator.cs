using API.Models;

namespace API.Services
{
    public interface ISourcesCalculator
    {
        EnergySourceResponse CalculateSourceEmissions(IEnumerable<TimeSeries> measurements, List<Record> records, Aggregation aggregation);
    }
}
