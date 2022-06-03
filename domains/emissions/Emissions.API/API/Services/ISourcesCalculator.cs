using API.Models;

namespace API.Services
{
    public interface ISourcesCalculator
    {
        EnergySourceResponse CalculateSourceEmissions(IEnumerable<TimeSeries> measurements, DeclarationProduction declaration, long dateFrom, long dateTo, Aggregation aggregation);
    }
}
