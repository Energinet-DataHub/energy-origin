using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using API.ReportGenerator.Domain;
using API.ReportGenerator.Processing;
using Microsoft.Extensions.Logging;

namespace API.ReportGenerator.Rendering;

public interface IEnergySvgRenderer
{
    EnergySvgResult Render(IReadOnlyList<HourlyEnergy> hourly);
}

public class EnergySvgRenderer() : IEnergySvgRenderer
{
    private const int SVG_WIDTH = 754;
    private const int SVG_HEIGHT = 400;
    private const int MARGIN_LEFT = 80;
    private const int MARGIN_TOP = 10;
    private const int PLOT_HEIGHT = 304;

    private const int SwitchToMegawattHoursThreshold = 1000;
    private const int SwitchToKilowattHoursThreshold = 10000;
    private const int EnergyUnitConversionFactor = 1000;

    private static readonly (string Unmatched, string Overmatched, string Matched,
        string AvgLine, string Bg, string HourText, string EnergyUnits)
        Colors = ("#DFDFDF", "#59ACE8", "#82CF76", "#FF0000", "#F9FAFB", "rgb(194,194,194)", "FFA500");

    private static readonly XNamespace svg = "http://www.w3.org/2000/svg";

    private enum EnergyUnit { WattHours, KilowattHour, MegawattHour };

    public EnergySvgResult Render(
        IReadOnlyList<HourlyEnergy> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var maxWattHours = SvgDataProcessor.MaxStackedWattHours(data);
        var minWattHours = SvgDataProcessor.MinStackedWattHours(data);
        var finalRender = CreateSvgDocument(data, maxWattHours, minWattHours).ToString();

        return new EnergySvgResult(finalRender);
    }

    private XDocument CreateSvgDocument(IReadOnlyList<HourlyEnergy> data, double maxValueWattHours, double minValueWattHours)
    {
        var unitWidth = (SVG_WIDTH - MARGIN_LEFT * 2) / 24.0;

        return new XDocument(
            new XElement(svg + "svg",
                new XAttribute("class", "highcharts-root"),
                new XAttribute("xmlns", svg),
                new XAttribute("viewBox", $"0 0 {SVG_WIDTH} {SVG_HEIGHT}"),
                new XAttribute("preserveAspectRatio", "xMidYMid meet"),
                new XAttribute("style", "width: 100%; height: auto; display: block;"),
                new XAttribute("role", "img"),

                CreateDefinitions(),
                CreateBackground(),
                CreateGridLines(unitWidth),
                CreateXAxisLine(),
                CreateChartSeries(data, unitWidth, maxValueWattHours),
                CreateXAxisLabels(data, unitWidth),
                CreateLegend(),
                CreateYAxisLabels(maxValueWattHours: maxValueWattHours, minValueWattHours: minValueWattHours),
                CreateYAxisLine()
            ));
    }

    private static XElement CreateChartSeries(IReadOnlyList<HourlyEnergy> data,
                                              double unitWidth,
                                              double maxValueWattHours)
    {
        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-series-group"),
            new XAttribute("data-z-index", "3"),
            new XAttribute("transform", $"translate({MARGIN_LEFT},{MARGIN_TOP})"),

            CreateBarSeries(data, unitWidth, maxValueWattHours, "unmatched", 0),
            CreateBarSeries(data, unitWidth, maxValueWattHours, "overmatched", 1),
            CreateBarSeries(data, unitWidth, maxValueWattHours, "matched", 2),
            CreateAverageLine(data, unitWidth, maxValueWattHours));
    }

    private static XElement CreateBarSeries(IEnumerable<HourlyEnergy> data,
                                            double unitWidth,
                                            double maxValue,
                                            string seriesType,
                                            int seriesIndex)
    {
        var colour = seriesType switch
        {
            "unmatched" => Colors.Unmatched,
            "overmatched" => Colors.Overmatched,
            "matched" => Colors.Matched,
            _ => throw new ArgumentException("Invalid argument value", nameof(seriesType))
        };

        return new XElement(svg + "g",
            new XAttribute("class",
                $"highcharts-series highcharts-series-{seriesIndex} highcharts-column-series"),
            new XAttribute("data-z-index", "0.1"),
            new XAttribute("opacity", "1"),
            new XAttribute("clip-path", "url(#chart-area)"),
            data.SelectMany(d => CreateBar(d, unitWidth, maxValue, seriesType, colour)));
    }

    private static IEnumerable<XElement> CreateBar(HourlyEnergy d,
                                                   double unitWidth,
                                                   double maxValue,
                                                   string seriesType,
                                                   string colour)
    {
        var value = seriesType switch
        {
            "unmatched" => d.Unmatched,
            "overmatched" => d.Overmatched,
            "matched" => d.Matched,
            _ => 0
        };
        if (value <= 0) yield break;

        var bw = unitWidth * 0.8;
        var xPos = unitWidth * d.Hour + unitWidth * 0.1;
        var height = PLOT_HEIGHT * value / maxValue;

        var yPos = seriesType == "matched"
            ? PLOT_HEIGHT - height
            : PLOT_HEIGHT - height - (PLOT_HEIGHT * d.Matched / maxValue);

        yield return new XElement(svg + "rect",
            new XAttribute("x", xPos.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y", yPos.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("width", bw.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("height", height.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("fill", colour));
    }

    private static XElement CreateAverageLine(IEnumerable<HourlyEnergy> data,
                                              double unitWidth,
                                              double maxValue)
    {
        return new XElement(svg + "g",
            new XAttribute("class",
                "highcharts-series highcharts-series-3 highcharts-line-series"),
            new XAttribute("data-z-index", "10"),
            new XAttribute("opacity", "1"),
            new XAttribute("clip-path", "url(#chart-area)"),
            new XElement(svg + "path",
                new XAttribute("fill", "none"),
                new XAttribute("class", "highcharts-graph"),
                new XAttribute("d", CreateAverageLinePath(data, unitWidth, maxValue)),
                new XAttribute("stroke", Colors.AvgLine),
                new XAttribute("stroke-width", "2"),
                new XAttribute("stroke-linejoin", "round"),
                new XAttribute("stroke-linecap", "round")));
    }

    private static string CreateAverageLinePath(IEnumerable<HourlyEnergy> data,
                                                    double unitWidth,
                                                  double maxValue)
    {
        var pts = data.OrderBy(d => d.Hour)
                                         .Select(d =>
                          {
                              var x = unitWidth * (d.Hour + 0.5);
                              var y = PLOT_HEIGHT - (PLOT_HEIGHT * d.Consumption / maxValue);
                              return $"{x.ToString(CultureInfo.InvariantCulture)}," +
                                                                       $"{y.ToString(CultureInfo.InvariantCulture)}";
                          });

        return "M " + string.Join(" ", pts);
    }

    private static XElement CreateXAxisLabels(IEnumerable<HourlyEnergy> data,
                                              double unitWidth)
    {
        return new XElement(svg + "g",
            new XAttribute("class",
                "highcharts-axis-labels highcharts-xaxis-labels"),
            new XAttribute("data-z-index", "7"),
            data.Select(d =>
            {
                var x = MARGIN_LEFT + unitWidth * (d.Hour + 0.5);
                return new XElement(svg + "text",
                    new XAttribute("x", x.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("y", MARGIN_TOP + PLOT_HEIGHT + 27),
                    new XAttribute("text-anchor", "middle"),
                    new XAttribute("style", $"cursor: default; font-size: 12px; fill: {Colors.HourText};"),
                    d.Hour.ToString("D2"));
            }));
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
        var y = (MARGIN_TOP + PLOT_HEIGHT + 0.5).ToString(CultureInfo.InvariantCulture);

        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-axis highcharts-xaxis"),
            new XAttribute("data-z-index", "2"),
            new XElement(svg + "path",
                new XAttribute("fill", "none"),
                new XAttribute("class", "highcharts-axis-line"),
                new XAttribute("stroke", "#ffffff"),
                new XAttribute("stroke-width", "1"),
                new XAttribute("d", $"M {MARGIN_LEFT} {y} L {SVG_WIDTH - MARGIN_LEFT} {y}")
            )
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

    private XElement CreateYAxisLabels(double maxValueWattHours, double minValueWattHours)
    {
        var energyUnit = GetEnergyUnitForYAxis(minValueWattHours);

        var maxValueKilowattHours = maxValueWattHours / EnergyUnitConversionFactor;
        switch (energyUnit)
        {
            case EnergyUnit.WattHours:
                return CreateYAxisLabelsForWattHours(maxValueWattHours);
            case EnergyUnit.KilowattHour:
                return CreateYAxisLabelsForKilowattHours(maxValueKilowattHours);
            case EnergyUnit.MegawattHour:
                var maxValueMegawattHours = maxValueKilowattHours / EnergyUnitConversionFactor;
                return CreateYAxisLabelsForMegawattHours(maxValueMegawattHours);
            default:
                throw new InvalidOperationException("Invalid EnergyUnit when creating report");
        }
    }

    private XElement CreateYAxisLabelsForWattHours(double maxValueWattHours)
    {
        var yAxisLabelCount = 5;
        var yAxisLabelInterval = maxValueWattHours / yAxisLabelCount;
        var roundToNearest = 5;

        var yAxisLabelValues = Enumerable.Range(0, yAxisLabelCount)
            .Select(i => Math.Round(i * yAxisLabelInterval / roundToNearest) * roundToNearest)
            .Concat([Math.Round(maxValueWattHours)])
            .OrderBy(v => v)
            .Distinct()
            .ToList();

        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-axis-labels highcharts-yaxis-labels"),
            new XAttribute("data-z-index", "7"),
            yAxisLabelValues.Select(value =>
            {
                var yAxisLabelPosition = MARGIN_TOP + PLOT_HEIGHT - (PLOT_HEIGHT * value / maxValueWattHours);
                return new XElement(svg + "text",
                    new XAttribute("x", MARGIN_LEFT - 5),
                    new XAttribute("y", yAxisLabelPosition),
                    new XAttribute("text-anchor", "end"),
                    new XAttribute("style", $"font-size: 12px; fill: {Colors.HourText};"),
                    value.ToString(CultureInfo.InvariantCulture) + $" Wh");
            })
        );
    }
    private XElement CreateYAxisLabelsForKilowattHours(double maxValueKwh)
    {
        var yAxisLabelCount = 5;
        var yAxisLabelInterval = maxValueKwh / yAxisLabelCount;
        var roundToNearest = 5;

        var yAxisLabelValues = Enumerable.Range(0, yAxisLabelCount)
            .Select(i => Math.Round(i * yAxisLabelInterval / roundToNearest) * roundToNearest)
            .Concat([Math.Round(maxValueKwh)])
            .OrderBy(v => v)
            .Distinct()
            .ToList();

        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-axis-labels highcharts-yaxis-labels"),
            new XAttribute("data-z-index", "7"),
            yAxisLabelValues.Select(value =>
            {
                var yAxisLabelPosition = MARGIN_TOP + PLOT_HEIGHT - (PLOT_HEIGHT * value / maxValueKwh);
                return new XElement(svg + "text",
                    new XAttribute("x", MARGIN_LEFT - 5),
                    new XAttribute("y", yAxisLabelPosition),
                    new XAttribute("text-anchor", "end"),
                    new XAttribute("style", $"font-size: 12px; fill: {Colors.HourText};"),
                    value.ToString(CultureInfo.InvariantCulture) + $" kWh");
            })
        );
    }

    private XElement CreateYAxisLabelsForMegawattHours(double maxValueMwh)
    {
        var yAxisLabelCount = 5;
        var yAxisLabelInterval = maxValueMwh / yAxisLabelCount;

        var yAxisLabelValues = Enumerable.Range(0, yAxisLabelCount)
            .Select(i => i * yAxisLabelInterval)
            .Concat([maxValueMwh])
            .OrderBy(v => v)
            .Distinct()
            .ToList();

        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-axis-labels highcharts-yaxis-labels"),
            new XAttribute("data-z-index", "7"),
            yAxisLabelValues.Select(value =>
            {
                var yAxisLabelPosition = MARGIN_TOP + PLOT_HEIGHT - (PLOT_HEIGHT * value / maxValueMwh);
                return new XElement(svg + "text",
                    new XAttribute("x", MARGIN_LEFT - 5),
                    new XAttribute("y", yAxisLabelPosition),
                    new XAttribute("text-anchor", "end"),
                    new XAttribute("style", $"font-size: 12px; fill: {Colors.HourText};"),
                    value.ToString("F2", CultureInfo.InvariantCulture) + $" MWh");
            })
        );
    }

    private static XElement CreateYAxisLine()
    {
        return new XElement(svg + "g",
            new XAttribute("class", "highcharts-axis highcharts-yaxis-left"),
            new XElement(svg + "line",
                new XAttribute("x1", MARGIN_LEFT),
                new XAttribute("y1", MARGIN_TOP),
                new XAttribute("x2", MARGIN_LEFT),
                new XAttribute("y2", MARGIN_TOP + PLOT_HEIGHT),
                new XAttribute("stroke", Colors.EnergyUnits),
                new XAttribute("stroke-width", "2")
            )
        );
    }

    private static EnergyUnit GetEnergyUnitForYAxis(double minValueWattHours)
    {
        if (minValueWattHours < SwitchToKilowattHoursThreshold)
        {
            return EnergyUnit.WattHours;
        }

        var minValueKilowattHours = minValueWattHours / EnergyUnitConversionFactor;
        if (minValueKilowattHours < SwitchToMegawattHoursThreshold)
        {
            return EnergyUnit.KilowattHour;
        }

        return EnergyUnit.MegawattHour;
    }
}
