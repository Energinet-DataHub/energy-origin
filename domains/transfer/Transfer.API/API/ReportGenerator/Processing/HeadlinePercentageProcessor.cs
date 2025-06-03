using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Domain;

namespace API.ReportGenerator.Processing;

public interface IHeadlinePercentageProcessor
{
    double Calculate(IReadOnlyList<HourlyEnergy> hours);
}

public class HeadlinePercentageProcessor : IHeadlinePercentageProcessor
{
    public double Calculate(IReadOnlyList<HourlyEnergy> hours)
    {
        if (!hours.Any()) return 0;

        var totalConsumed = hours.Sum(h => h.Consumption);
        var totalMatched = hours.Sum(h => h.Matched);

        if (totalConsumed == 0) return 100;
        return totalMatched / totalConsumed * 100;
    }
}
