using System;

namespace API.ReportGenerator.Rendering;

public interface IOrganizationHeaderRenderer
{
    string Render(string organizationName, string taxIdentificationNumber);
}

public class OrganizationHeaderRenderer : IOrganizationHeaderRenderer
{
    public string Render(string organizationName, string taxIdentificationNumber)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
            throw new ArgumentException("Organization name cannot be empty", nameof(organizationName));

        if (string.IsNullOrWhiteSpace(taxIdentificationNumber))
            throw new ArgumentException("Tax identification number cannot be empty", nameof(taxIdentificationNumber));

        // Extract first letter for the styled span
        var firstLetter = organizationName.Substring(0, 1);
        var restOfName = organizationName.Substring(1);

        var html = $"""
                    <div data-layer="Top" class="top">
                        <div data-layer="{organizationName}" class="organization-name">
                            <span style="color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 700; padding-left: 12px; text-transform: uppercase; line-height: 16px; letter-spacing: 1.20px; word-wrap: break-word">{firstLetter}</span><span style="color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 700; line-height: 16px; letter-spacing: 1.20px; word-wrap: break-word">{restOfName}</span>
                        </div>
                        <div data-layer="{taxIdentificationNumber}" class="tin" style="color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 400; padding-right: 12px; line-height: 16px; letter-spacing: 1.20px; word-wrap: break-word">
                            CVR {taxIdentificationNumber}
                        </div>
                    </div>
                    <div data-layer="header" class="header" style="position: relative;padding-bottom: 22px;">
                        <div data-layer="Frame 17" class="Frame17" style="width: 531px; height: 33px; padding-left: 0px; padding-right: 18px; left: 14px; top: 0px; position: absolute; justify-content: flex-start; align-items: flex-start; display: inline-flex">
                            <div data-layer="Markedsrapport baseret på" class="MarkedsrapportBaseretP" style="color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 400; letter-spacing: 1.20px; word-wrap: break-word">
                                Markedsrapport baseret på
                            </div>
                        </div>
                        <div data-layer="Granulære oprindelsesgarantier" class="title">
                            Granulære oprindelsesgarantier
                        </div>
                    </div>
                    """;

        return html.Trim();
    }
}
