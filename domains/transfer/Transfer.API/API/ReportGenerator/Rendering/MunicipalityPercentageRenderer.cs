using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using API.ReportGenerator.Domain;
using EnergyOrigin.Domain.ValueObjects.Mappers;

namespace API.ReportGenerator.Rendering;

public interface IMunicipalityPercentageRenderer
{
    string Render(List<MunicipalityDistribution> municipalities);
}

public class MunicipalityPercentageRenderer : IMunicipalityPercentageRenderer
{
    public string Render(List<MunicipalityDistribution> municipalities)
    {
        if (municipalities.Count == 0)
            return "<p>Ingen kommunedata tilgængelig</p>";

        var ordered = municipalities.OrderByDescending(x => x.Percentage).ToList();
        var top3 = ordered.Take(3);
        var rest = ordered.Skip(3);

        var html = @"<h6 class=""section-title"">Kommuner</h6><ul>";

        foreach (var m in top3)
        {
            var name = MunicipalityCodeMapper.GetMunicipalityName(m.Municipality);
            if (!string.IsNullOrWhiteSpace(name))
            {
                var safeName = System.Net.WebUtility.HtmlEncode(name);
                var formattedPct = m.Percentage.ToString("0.0", CultureInfo.InvariantCulture);
                html += $"<li>{safeName}: {formattedPct}%</li>";
            }
        }

        var restSum = rest.Sum(x => x.Percentage);
        var formattedRest = restSum.ToString("0.0", CultureInfo.InvariantCulture);
        html += $"<li>Andre kommuner: {formattedRest}%</li>";

        html += "</ul>";

        return html.Trim();
    }
}
