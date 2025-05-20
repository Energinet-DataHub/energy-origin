using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using API.Transfer.Api.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;

namespace API.Report;

public record DataPoint(DateTime Timestamp, double Value);
public record Metrics(double Hourly, double Daily, double Weekly, double Monthly, double Annual);
public record EnergySvgResult(string Svg, Metrics Metrics);

public static class EnergyChartGenerator
{
    private const int SVG_WIDTH = 754;
    private const int SVG_HEIGHT = 400;
    private const int MARGIN_LEFT = 10;
    private const int MARGIN_TOP = 10;
    private const int PLOT_HEIGHT = 304;

    private static readonly (string Unmatched, string Overmatched, string Matched, string AvgLine, string Bg, string HourText)
        Colors = ("#DFDFDF", "#59ACE8", "#82CF76", "#FF0000", "#F9FAFB", "rgb(194,194,194)");

    private static readonly XNamespace svg = "http://www.w3.org/2000/svg";

    public static EnergySvgResult GenerateEnergySvg(
        IEnumerable<DataPoint> consumption,
        IEnumerable<DataPoint> production,
        Metrics? metrics = null)
    {
        metrics ??= new Metrics(50, 77, 88, 95, 100);

        var hourlyData = ProcessHourlyData(consumption, production);
        var maxValue = CalculateMaxValue(hourlyData);

        return new EnergySvgResult(
            CreateSvgDocument(hourlyData, maxValue).ToString(),
            metrics
        );
    }

    private static List<(int Hour, double Consumption, double Matched, double Unmatched, double Overmatched)>
        ProcessHourlyData(IEnumerable<DataPoint> consumption, IEnumerable<DataPoint> production)
    {
        var consLookup = consumption.ToLookup(d => d.Timestamp.Hour, d => d.Value);
        var prodLookup = production.ToLookup(d => d.Timestamp.Hour, d => d.Value);

        return Enumerable.Range(0, 24)
            .Select(h => (
                Hour: h,
                Consumption: consLookup[h].DefaultIfEmpty().Average(),
                Production: prodLookup[h].DefaultIfEmpty().Average()
            ))
            .Select(t => (
                t.Hour,
                t.Consumption,
                Matched: Math.Min(t.Consumption, t.Production),
                Unmatched: Math.Max(0, t.Consumption - t.Production),
                Overmatched: Math.Max(0, t.Production - t.Consumption)
            ))
            .ToList();
    }

    private static double CalculateMaxValue(
        IEnumerable<(int Hour, double Consumption, double Matched, double Unmatched, double Overmatched)> data)
    {
        return data.Max(t => Math.Max(t.Consumption, t.Matched + t.Unmatched + t.Overmatched));
    }

    private static XDocument CreateSvgDocument(
        List<(int Hour, double Consumption, double Matched, double Unmatched, double Overmatched)> data,
        double maxValue)
    {
        var unitWidth = (SVG_WIDTH - MARGIN_LEFT * 2) / 24.0;

        return new XDocument(
            new XElement(svg + "svg",
                new XAttribute("class", "highcharts-root"),
                new XAttribute("xmlns", "http://www.w3.org/2000/svg"),
                new XAttribute("viewBox", $"0 0 {SVG_WIDTH} {SVG_HEIGHT}"),
                new XAttribute("preserveAspectRatio", "xMidYMid meet"),
                new XAttribute("style", "width: 100%; height: auto; display: block;"),
                new XAttribute("role", "img"),
                new XAttribute("aria-label", ""),

                CreateDefinitions(),
                CreateBackground(),
                CreateGridLines(unitWidth),
                CreateXAxisLine(),
                CreateChartSeries(data, unitWidth, maxValue),
                CreateXAxisLabels(data, unitWidth),
                CreateLegend()
            )
        );
    }

    private static XElement CreateDefinitions()
    {
        return new XElement(svg + "defs",
            new XElement(svg + "filter",
                new XAttribute("id", "drop-shadow"),
                new XElement(svg + "feDropShadow",
                    new XAttribute("dx", "1"),
                    new XAttribute("dy", "1"),
                    new XAttribute("flood-color", "#000000"),
                    new XAttribute("flood-opacity", "0.75"),
                    new XAttribute("stdDeviation", "2.5")
                )
            ),
            new XElement(svg + "clipPath",
                new XAttribute("id", "chart-area"),
                new XElement(svg + "rect",
                    new XAttribute("x", "0"),
                    new XAttribute("y", "0"),
                    new XAttribute("width", SVG_WIDTH - MARGIN_LEFT * 2),
                    new XAttribute("height", PLOT_HEIGHT),
                    new XAttribute("fill", "none")
                )
            )
        );
    }

    private static XElement CreateBackground()
    {
        return new XElement(svg + "rect",
            new XAttribute("fill", Colors.Bg),
            new XAttribute("class", "highcharts-background"),
            new XAttribute("x", "0"),
            new XAttribute("y", "0"),
            new XAttribute("width", SVG_WIDTH),
            new XAttribute("height", SVG_HEIGHT),
            new XAttribute("rx", "0"),
            new XAttribute("ry", "0")
        );
    }

    private static XElement CreateGridLines(double unitWidth)
    {
        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-grid highcharts-xaxis-grid"),
            new XAttribute("data-z-index", "1"),
            Enumerable.Range(0, 25).Select(i =>
            {
                double x = MARGIN_LEFT + unitWidth * i;
                return new XElement(svg + "path",
                    new XAttribute("fill", "none"),
                    new XAttribute("stroke", "#e6e6e6"),
                    new XAttribute("stroke-width", "0"),
                    new XAttribute("data-z-index", "1"),
                    new XAttribute("class", "highcharts-grid-line"),
                    new XAttribute("d", $"M {x.ToString(CultureInfo.InvariantCulture)} {MARGIN_TOP} L {x.ToString(CultureInfo.InvariantCulture)} {MARGIN_TOP + PLOT_HEIGHT}"),
                    new XAttribute("opacity", "1")
                );
            })
        );
    }

    private static XElement CreateXAxisLine()
    {
        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-axis highcharts-xaxis"),
            new XAttribute("data-z-index", "2"),
            new XElement(svg + "path",
                new XAttribute("fill", "none"),
                new XAttribute("class", "highcharts-axis-line"),
                new XAttribute("stroke", "#ffffff"),
                new XAttribute("stroke-width", "1"),
                new XAttribute("d", $"M {MARGIN_LEFT} {MARGIN_TOP + PLOT_HEIGHT + 0.5} L {SVG_WIDTH - MARGIN_LEFT} {MARGIN_TOP + PLOT_HEIGHT + 0.5}")
            )
        );
    }

    private static XElement CreateChartSeries(
        List<(int Hour, double Consumption, double Matched, double Unmatched, double Overmatched)> data,
        double unitWidth,
        double maxValue)
    {
        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-series-group"),
            new XAttribute("data-z-index", "3"),
            new XAttribute("transform", $"translate({MARGIN_LEFT},{MARGIN_TOP})"),

            // Unmatched series (gray)
            CreateBarSeries(data, unitWidth, maxValue, "unmatched", 0),
            // Overmatched series (blue)
            CreateBarSeries(data, unitWidth, maxValue, "overmatched", 1),
            // Matched series (green)
            CreateBarSeries(data, unitWidth, maxValue, "matched", 2),
            // Average line (red)
            new XElement(svg + "g",
                new XAttribute("class", "highcharts-series highcharts-series-3 highcharts-line-series"),
                new XAttribute("data-z-index", "10"),
                new XAttribute("opacity", "1"),
                new XAttribute("clip-path", "url(#chart-area)"),
                new XElement(svg + "path",
                    new XAttribute("fill", "none"),
                    new XAttribute("d", CreateAverageLinePath(data, unitWidth, maxValue)),
                    new XAttribute("class", "highcharts-graph"),
                    new XAttribute("data-z-index", "1"),
                    new XAttribute("stroke", Colors.AvgLine),
                    new XAttribute("stroke-width", "2"),
                    new XAttribute("stroke-linejoin", "round"),
                    new XAttribute("stroke-linecap", "round")
                )
            )
        );
    }

    private static XElement CreateBarSeries(
        IEnumerable<(int Hour, double Consumption, double Matched, double Unmatched, double Overmatched)> data,
        double unitWidth,
        double maxValue,
        string seriesType,
        int seriesIndex)
    {
        var color = seriesType switch
        {
            "unmatched" => Colors.Unmatched,
            "overmatched" => Colors.Overmatched,
            "matched" => Colors.Matched,
            _ => throw new ArgumentException("Invalid series type")
        };

        return new XElement(svg + "g",
            new XAttribute("class", $"highcharts-series highcharts-series-{seriesIndex} highcharts-column-series"),
            new XAttribute("data-z-index", "0.1"),
            new XAttribute("opacity", "1"),
            new XAttribute("clip-path", "url(#chart-area)"),
            data.SelectMany(t => CreateBar(t, unitWidth, maxValue, seriesType, color))
        );
    }

    private static IEnumerable<XElement> CreateBar(
        (int Hour, double Consumption, double Matched, double Unmatched, double Overmatched) data,
        double unitWidth,
        double maxValue,
        string seriesType,
        string color)
    {
        var value = seriesType switch
        {
            "unmatched" => data.Unmatched,
            "overmatched" => data.Overmatched,
            "matched" => data.Matched,
            _ => 0
        };

        if (value <= 0) yield break;

        var barWidth = unitWidth * 0.8;
        var xPos = unitWidth * data.Hour + unitWidth * 0.1;
        var height = PLOT_HEIGHT * value / maxValue;

        // Calculate y position based on series type
        double yPos;

        if (seriesType == "matched")
        {
            yPos = PLOT_HEIGHT - height;
        }
        else if (seriesType == "unmatched")
        {
            yPos = PLOT_HEIGHT - height - (PLOT_HEIGHT * data.Matched / maxValue);
        }
        else
        {
            yPos = PLOT_HEIGHT - height - (PLOT_HEIGHT * data.Matched / maxValue);
        }

        yield return new XElement(svg + "rect",
            new XAttribute("x", xPos.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y", yPos.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("width", barWidth.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("height", height.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("fill", color)
        );
    }

    private static string CreateAverageLinePath(
        List<(int Hour, double Consumption, double Matched, double Unmatched, double Overmatched)> data,
        double unitWidth,
        double maxValue)
    {
        var points = data
            .OrderBy(d => d.Hour)
            .Select(d =>
            {
                var x = unitWidth * (d.Hour + 0.5);
                var y = PLOT_HEIGHT - (PLOT_HEIGHT * d.Consumption / maxValue);
                return $"{x.ToString(NumberFormatInfo.InvariantInfo)},{y.ToString(NumberFormatInfo.InvariantInfo)}";
            });

        return "M " + string.Join(" ", points);
    }

    private static XElement CreateXAxisLabels(
        IEnumerable<(int Hour, double Consumption, double Matched, double Unmatched, double Overmatched)> data,
        double unitWidth)
    {
        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-axis-labels highcharts-xaxis-labels"),
            new XAttribute("data-z-index", "7"),
            data.Select(t =>
            {
                double x = MARGIN_LEFT + unitWidth * (t.Hour + 0.5);
                return new XElement(svg + "text",
                    new XAttribute("x", x.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("style", "cursor: default; font-size: 12px; fill: " + Colors.HourText + ";"),
                    new XAttribute("text-anchor", "middle"),
                    new XAttribute("transform", "translate(0,0)"),
                    new XAttribute("y", MARGIN_TOP + PLOT_HEIGHT + 27),
                    new XAttribute("opacity", "1"),
                    t.Hour.ToString("D2")
                );
            })
        );
    }

    private static XElement CreateLegend()
    {
        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-legend"),
            new XAttribute("data-z-index", "7"),
            new XAttribute("transform", "translate(119,355)"),
            new XElement(svg + "rect",
                new XAttribute("fill", "none"),
                new XAttribute("class", "highcharts-legend-box"),
                new XAttribute("rx", "0"),
                new XAttribute("ry", "0"),
                new XAttribute("stroke", "#999999"),
                new XAttribute("stroke-width", "0"),
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("width", "517"),
                new XAttribute("height", "30")),
            CreateLegendItems()
        );
    }

    private static XElement CreateLegendItems()
    {
        var items = new[]
        {
            ("Ikke matchet", Colors.Unmatched, false),
            ("Overmatchet", Colors.Overmatched, false),
            ("Matchet", Colors.Matched, false),
            ("Årligt forbrug", Colors.AvgLine, true)
        };

        return new XElement(svg + "g",
            new XAttribute("data-z-index", "1"),
            new XElement(svg + "g",
                items.Select((item, index) =>
                    new XElement(svg + "g",
                        new XAttribute("class", item.Item3 ?
                            "highcharts-legend-item highcharts-line-series" :
                            "highcharts-legend-item highcharts-column-series"),
                        new XAttribute("data-z-index", "1"),
                        new XAttribute("transform", $"translate({8 + index * 120},3)"),
                        item.Item3 ? CreateLineLegendItem(item) : CreateRectLegendItem(item))
                )
            )
        );
    }

    private static IEnumerable<XElement> CreateRectLegendItem((string Label, string Color, bool IsLine) item)
    {
        yield return new XElement(svg + "text",
            new XAttribute("x", "21"),
            new XAttribute("y", "17"),
            new XAttribute("text-anchor", "start"),
            new XAttribute("data-z-index", "2"),
            new XAttribute("style", "font-size: 12px; font-family: OpenSansNormal; fill: rgb(51, 51, 51);"),
            item.Label);

        yield return new XElement(svg + "rect",
            new XAttribute("x", "2"),
            new XAttribute("y", "6"),
            new XAttribute("rx", "6"),
            new XAttribute("ry", "6"),
            new XAttribute("width", "12"),
            new XAttribute("height", "12"),
            new XAttribute("fill", item.Color),
            new XAttribute("class", "highcharts-point"),
            new XAttribute("data-z-index", "3"));
    }

    private static IEnumerable<XElement> CreateLineLegendItem((string Label, string Color, bool IsLine) item)
    {
        yield return new XElement(svg + "path",
            new XAttribute("fill", "none"),
            new XAttribute("class", "highcharts-graph"),
            new XAttribute("stroke-width", "2"),
            new XAttribute("stroke-linecap", "round"),
            new XAttribute("d", "M 1 13 L 15 13"),
            new XAttribute("stroke", item.Color));

        yield return new XElement(svg + "text",
            new XAttribute("x", "21"),
            new XAttribute("y", "17"),
            new XAttribute("text-anchor", "start"),
            new XAttribute("data-z-index", "2"),
            new XAttribute("style", "font-size: 12px; font-family: OpenSansNormal; fill: rgb(51, 51, 51);"),
            item.Label);
    }

    public static async Task<EnergySvgResult> GenerateEnergySvgAsync(
        IConsumptionService consumptionService,
        IWalletClient walletClient,
        OrganizationId organizationId,
        DateTimeOffset from,
        DateTimeOffset to,
        Metrics? metrics = null)
    {
        var consTask = consumptionService.GetTotalHourlyConsumption(organizationId, from, to, CancellationToken.None);
        var claimsTask = walletClient.GetClaims(organizationId.Value, from, to, CancellationToken.None);

        await Task.WhenAll(consTask, claimsTask);

        var consumptionData = (await consTask)
            .OrderBy(ch => ch.HourOfDay)
            .Select(ch => new DataPoint(from.Date.AddHours(ch.HourOfDay), (double)ch.KwhQuantity));

        var claims = (await claimsTask)?.Result ?? Enumerable.Empty<Claim>();
        var productionData = claims
            .GroupBy(c => DateTimeOffset.FromUnixTimeSeconds(c.UpdatedAt).Hour)
            .SelectMany(g => g.Select(c => new DataPoint(from.Date.AddHours(g.Key), c.Quantity)));

        return GenerateEnergySvg(consumptionData, productionData, metrics);
    }
}
