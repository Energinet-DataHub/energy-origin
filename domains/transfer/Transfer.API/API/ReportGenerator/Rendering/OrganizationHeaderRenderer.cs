using System;
using DataContext.Models;

namespace API.ReportGenerator.Rendering;

public interface IOrganizationHeaderRenderer
{
    string Render(string organizationName, string taxIdentificationNumber, Language language);
}

public sealed class OrganizationHeaderRenderer : IOrganizationHeaderRenderer
{
    public string Render(string organizationName, string taxIdentificationNumber, Language language)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
            throw new ArgumentException("Organization name cannot be empty", nameof(organizationName));

        if (string.IsNullOrWhiteSpace(taxIdentificationNumber))
            throw new ArgumentException("Tax identification number cannot be empty", nameof(taxIdentificationNumber));

        var labels = HeaderLabels.From(language);
        var firstLetter = organizationName.Substring(0, 1);
        var restOfName = organizationName.Substring(1);

        return $"""
            <div data-layer="Top" class="Top" style="width: 563px; padding-left: 16px; padding-right: 16px; padding-top: 10px; padding-bottom: 10px; justify-content: space-between; align-items: center; display: inline-flex">
                <div data-layer="{organizationName}" class="organization-name">
                    <span style="color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 700; padding-left: 12px; text-transform: uppercase; line-height: 16px; letter-spacing: 1.20px; word-wrap: break-word">{firstLetter}</span>
                    <span style="color: #002433; font-size: 12px; font-family: Open Sans; font-weight: 700; line-height: 16px; letter-spacing: 1.20px; word-wrap: break-word">{restOfName}</span>
                </div>
                <div data-layer="{taxIdentificationNumber}" class="tin" style="color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 400; padding-right: 12px; line-height: 16px; letter-spacing: 1.20px; word-wrap: break-word">{taxIdentificationNumber}</div>
            </div>
            <div data-layer="header" class="header" style="width: 531px;height: 104px;position: relative;padding-bottom: 22px;">
                <div data-layer="Frame 17" class="Frame17" style="width: 531px; height: 33px; padding-left: 0px; padding-right: 18px; left: 14px; top: 0px; position: absolute; justify-content: flex-start; align-items: flex-start; display: inline-flex">
                    <div data-layer="subheading" class="MarkedsrapportBaseretP" style="color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 400; letter-spacing: 1.20px; word-wrap: break-word">{labels.Subheading}</div>
                </div>
                <div data-layer="heading" class="GranulReOprindelsesgarantier" style="width: 531px; left: 14px; top: 22px; padding-bottom: 32px; position: relative; color: #002433; font-size: 47px; font-family: OpenSansBold; font-weight: 700; line-height: 51.70px; word-wrap: break-word">{labels.Title}</div>
            </div>
            """.Trim();
    }
}

public sealed record HeaderLabels
{
    public required string Subheading { get; init; }
    public required string Title { get; init; }

    public static HeaderLabels From(Language language) => language switch
    {
        Language.English => new HeaderLabels
        {
            Subheading = "Market report based on",
            Title = "Granular Guarantees of Origin"
        },
        Language.Danish => new HeaderLabels
        {
            Subheading = "Markedsrapport baseret på",
            Title = "Granulære oprindelsesgarantier"
        },
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language")
    };
}
