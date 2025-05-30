using System;
using DataContext.Models;

namespace API.ReportGenerator.Rendering;

public interface IHeadlinePercentageRenderer
{
    string Render(double percent, string period, Language language);
}

public sealed class HeadlinePercentageRenderer : IHeadlinePercentageRenderer
{
    public string Render(double percent, string period, Language language)
    {
        var pct = Math.Round(percent).ToString("0");

        var (timeCoverage, periodLabel) = language switch
        {
            Language.English => ("Hourly Coverage", "For the period"),
            Language.Danish => ("Timedækning", "For perioden"),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language")
        };

        var html = $"""
                        <div data-layer="Grapth Top" class="GrapthTop" style="width: 562.99px; height: 75px; position: relative; background-color: #F9FAFB;">
                            <div data-layer="{periodLabel} {period}" class="ForPeriodenRet2024" style="right: 16.07px; top: 19px; position: absolute; text-align: right; line-height: 1.1;">
                                <span style="color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 400; text-transform: uppercase; letter-spacing: 0.60px; word-wrap: break-word">{periodLabel}<br/></span>
                                <span style="color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 800; text-transform: uppercase; letter-spacing: 0.60px; word-wrap: break-word">{period}</span>
                            </div>
                            <div data-layer="{pct}%" style="width: 117px; height: 66px; left: 16px; top: 28px; position: absolute; color: #002433; font-size: 44px; font-family: OpenSansBold; font-weight: 800; word-wrap: break-word">{pct}%</div>
                            <div data-layer="{timeCoverage}" class="TimedKning" style="width: 209.87px; left: 16px; top: 19px; position: absolute; color: #002433; font-size: 12px; font-family: OpenSansNormal; font-weight: 400; text-transform: uppercase; letter-spacing: 0.60px; word-wrap: break-word">{timeCoverage}</div>
                        </div>
                    """;

        return html.Trim();
    }
}
