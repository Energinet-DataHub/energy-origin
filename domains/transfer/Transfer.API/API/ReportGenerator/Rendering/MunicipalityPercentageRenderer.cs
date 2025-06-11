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
        var ordered = municipalities.OrderByDescending(x => x.Percentage).ToList();
        var top3Municipalities = ordered.Take(3);
        var rest = ordered.Skip(3);

        var html2 = @"<h6 class=""section-title"">Kommuner</h6>
                    <ul>";

        foreach (var municipality in top3Municipalities)
        {
            var name = MunicipalityCodeMapper.GetMunicipalityName(municipality.Municipality);
            if (name != null)
                html2 += $"<li>{name}: {municipality.Percentage:0}%</li>";
        }

        html2 += $"<li>Andre kommuner: {rest.Sum(x => x.Percentage):0}%</li>";

        html2 += "</ul>";

        return html2.Trim();
    }
}
