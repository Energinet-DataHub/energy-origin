using System.Collections.Generic;
using API.ReportGenerator.Domain;

namespace API.ReportGenerator.Rendering;

public interface IEnergySvgRenderer
{
    EnergySvgResult Render(IReadOnlyList<HourlyEnergy> hourly, Metrics? metrics = null);
}
