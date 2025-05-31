using System;
using DataContext.Models;

namespace API.ReportGenerator.Rendering;

public interface IDetailsRenderer
{
    string Render(Language language);
}

public sealed class DetailsRenderer : IDetailsRenderer
{
    public string Render(Language language)
    {
        var detailsLabels = DetailsLabels.From(language);

        return $"""
                    <div class="details">
                        <p class="description">{detailsLabels.Description}</p>

                        <div class="sections">
                            <div class="section-column">
                                <h6 class="section-title">{detailsLabels.Municipalities}</h6>
                                <ul>
                                    <li>Aabenraa: 50%</li>
                                    <li>Ringkøbing-Skjern: 25%</li>
                                    <li>Lolland: 20%</li>
                                    <li>{detailsLabels.OtherMunicipalities}: 5%</li>
                                </ul>
                            </div>
                            <div class="section-column">
                                <h6 class="section-title">{detailsLabels.Technology}</h6>
                                <ul>
                                    <li>{detailsLabels.Solar}: 38%</li>
                                    <li>{detailsLabels.Wind}: 62%</li>
                                </ul>
                            </div>
                            <div class="section-column">
                                <h6 class="section-title">{detailsLabels.SupportTitle}</h6>
                                <ul>
                                    <li>{detailsLabels.Unsupported}: 95%</li>
                                    <li>{detailsLabels.Supported}: 5%</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                """.Trim();
    }
}

public sealed record DetailsLabels
{
    public required string Description { get; init; }
    public required string Municipalities { get; init; }
    public required string OtherMunicipalities { get; init; }
    public required string Technology { get; init; }
    public required string Solar { get; init; }
    public required string Wind { get; init; }
    public required string SupportTitle { get; init; }
    public required string Unsupported { get; init; }
    public required string Supported { get; init; }

    public static DetailsLabels From(Language language) => language switch
    {
        Language.English => new DetailsLabels
        {
            Description = "Granular Guarantees of Origin are issued exclusively based on solar and wind production. Below you can see a breakdown of geographical origin, technology types, and the share from state-supported producers.",
            Municipalities = "Municipalities",
            OtherMunicipalities = "Other municipalities",
            Technology = "Technology",
            Solar = "Solar energy",
            Wind = "Wind energy",
            SupportTitle = "Share from state-sponsored producers",
            Unsupported = "Non state-sponsored",
            Supported = "State-sponsored"
        },
        Language.Danish => new DetailsLabels
        {
            Description = "Granulære Oprindelsesgarantier er udelukkende udstedt på basis af sol- og vindproduktion. Herunder kan du se en fordeling af geografisk oprindelse, teknologityper samt andelen fra statsstøttede producenter.",
            Municipalities = "Kommuner",
            OtherMunicipalities = "Andre kommuner",
            Technology = "Teknologi",
            Solar = "Solenergi",
            Wind = "Vindenergi",
            SupportTitle = "Andel fra statsstøttede producenter",
            Unsupported = "Ikke statsstøttede",
            Supported = "Statsstøttede"
        },
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language")
    };
}
