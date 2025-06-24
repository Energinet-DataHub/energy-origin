using System;
using System.Collections.Generic;
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
        var orderedWithoutNull = municipalities.Where(x => !string.IsNullOrWhiteSpace(x.Municipality)).OrderByDescending(x => x.Percentage).ToList();
        var top3Municipalities = orderedWithoutNull.Take(3);
        var rest = orderedWithoutNull.Skip(3).ToList();
        rest.AddRange(municipalities.Where(x => string.IsNullOrWhiteSpace(x.Municipality)));

        var html2 = @"<h6 class=""section-title"">Kommuner</h6>
                    <ul>";

        foreach (var municipality in top3Municipalities)
        {
            var name = municipality.Municipality != null ? MunicipalityCodeMapper.GetMunicipalityName(municipality.Municipality) : null;
            if (name != null)
                html2 += $"<li>{name}: {municipality.Percentage:0}%</li>";
        }

        html2 += $"<li>Andre kommuner: {rest.Sum(x => x.Percentage):0}%</li>";

        html2 += "</ul>";

        return html2.Trim();
    }
}
