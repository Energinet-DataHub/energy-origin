using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Domain;

namespace API.ReportGenerator.Processing;

public interface IHeadlinePercentageProcessor
{
    double Render(IReadOnlyList<HourlyEnergy> hours);
}

public sealed class HeadlinePercentageProcessor : IHeadlinePercentageProcessor
{
    public double Render(IReadOnlyList<HourlyEnergy> hours)
    {
        if (hours.Count == 0) return 0;

        var fullyMatched = hours.Count(h => h.Unmatched == 0);
        return fullyMatched / (double)hours.Count * 100;
    }
}
