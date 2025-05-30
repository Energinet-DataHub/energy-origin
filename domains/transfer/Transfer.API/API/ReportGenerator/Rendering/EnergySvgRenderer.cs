using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using API.ReportGenerator.Domain;
using API.ReportGenerator.Processing;
using DataContext.Models;

namespace API.ReportGenerator.Rendering;

public interface IEnergySvgRenderer
{
    EnergySvgResult Render(IReadOnlyList<HourlyEnergy> hourly, Language language);
}

public sealed class EnergySvgRenderer : IEnergySvgRenderer
{
    private static readonly XNamespace svg = "http://www.w3.org/2000/svg";

    private const int SvgWidth = 754;
    private const int SvgHeight = 400;
    private const int MarginLeft = 10;
    private const int MarginTop = 10;
    private const int PlotHeight = 304;
    private const int LegendTop = 355;
    private const int LegendLeft = 119;
    private const double BarGapPct = .1;     // 10 % gap left & right → barWidth = 0.8*unitWidth

    private static class Color
    {
        public const string Unmatched = "#DFDFDF";
        public const string Overmatched = "#59ACE8";
        public const string Matched = "#82CF76";
        public const string AvgLine = "#FF0000";
        public const string Background = "#F9FAFB";
        public const string AxisText = "rgb(194,194,194)";
    }

    private static string ToInvariant(double d) => d.ToString(CultureInfo.InvariantCulture);

    private readonly record struct SeriesSpec(
        Func<HourlyEnergy, double> Selector,
        string Colour,
        Func<Language, string> LocalizedLabel
        );


    // **Order = stacking order bottom → top**
    private static readonly SeriesSpec[] Series =
    {
        new(h => h.Matched,    Color.Matched,    lang => lang == Language.Danish ? "Matchet"     : "Matched"),
        new(h => h.Unmatched,  Color.Unmatched,  lang => lang == Language.Danish ? "Ikke matchet"   : "Unmatched"),
        new(h => h.Overmatched,Color.Overmatched,lang => lang == Language.Danish ? "Overmatchet" : "Overmatched")
    };

    public EnergySvgResult Render(IReadOnlyList<HourlyEnergy> data, Language language)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Count != 24)
            throw new ArgumentException("Hourly array must contain exactly 24 elements.", nameof(data));

        double maxStack = EnergyDataProcessor.MaxStacked(data);

        var unitWidth = (SvgWidth - MarginLeft * 2) / 24.0;
        var barWidth = unitWidth * (1 - 2 * BarGapPct);
        var barXStart = unitWidth * BarGapPct;

        var svgDoc =
            new XDocument(
                new XElement(svg + "svg",
                    new XAttribute("xmlns", svg),
                    new XAttribute("viewBox", $"0 0 {SvgWidth} {SvgHeight}"),
                    new XAttribute("preserveAspectRatio", "xMidYMid meet"),
                    new XAttribute("style", "width:100%;height:auto;display:block;"),
                    CreateDefs(),
                    CreateBackground(),
                    CreateGrid(unitWidth),
                    CreateXAxisLine(),
                    CreateBars(data, maxStack, unitWidth, barWidth, barXStart),
                    CreateAverageLine(data, maxStack, unitWidth),
                    CreateXAxisLabels(unitWidth),
                    CreateLegend(language)
                ));

        return new EnergySvgResult(svgDoc.ToString());
    }

    private static XElement CreateBackground() =>
        new(svg + "rect",
            new XAttribute("fill", Color.Background),
            new XAttribute("x", 0), new XAttribute("y", 0),
            new XAttribute("width", SvgWidth),
            new XAttribute("height", SvgHeight));

    private static XElement CreateDefs() =>
        new(svg + "defs",
            new XElement(svg + "clipPath",
                new XAttribute("id", "plot"),
                new XElement(svg + "rect",
                    new XAttribute("x", 0),
                    new XAttribute("y", 0),
                    new XAttribute("width", SvgWidth - MarginLeft * 2),
                    new XAttribute("height", PlotHeight))));

    private static XElement CreateGrid(double unitWidth)
    {
        var lines = Enumerable.Range(0, 25).Select(i =>
        {
            double x = MarginLeft + unitWidth * i;
            return new XElement(svg + "line",
                new XAttribute("x1", ToInvariant(x)),
                new XAttribute("y1", MarginTop),
                new XAttribute("x2", ToInvariant(x)),
                new XAttribute("y2", MarginTop + PlotHeight),
                new XAttribute("stroke", "#e6e6e6"),
                new XAttribute("stroke-width", 0));
        });
        return new(svg + "g", lines);
    }

    private static XElement CreateXAxisLine() =>
        new(svg + "line",
            new XAttribute("x1", MarginLeft),
            new XAttribute("y1", MarginTop + PlotHeight + 0.5),
            new XAttribute("x2", SvgWidth - MarginLeft),
            new XAttribute("y2", MarginTop + PlotHeight + 0.5),
            new XAttribute("stroke", "#ffffff"),
            new XAttribute("stroke-width", 1));

    private static XElement CreateBars(IEnumerable<HourlyEnergy> data,
                                       double maxStack,
                                       double unitWidth,
                                       double barWidth,
                                       double barXStart)
    {
        const double R = 1.0;

        string RoundedTopPath(double x, double y, double w, double h) =>
            $"M {ToInvariant(x)} {ToInvariant(y + R)} " +
            $"a {R} {R} 0 0 1 {ToInvariant(R)} {ToInvariant(-R)} " +
            $"h {ToInvariant(w - 2 * R)} " +
            $"a {R} {R} 0 0 1 {ToInvariant(R)} {ToInvariant(R)} " +
            $"v {ToInvariant(h - R)} h -{ToInvariant(w)} Z";

        string FlatRectPath(double x, double y, double w, double h) =>
            $"M {ToInvariant(x)} {ToInvariant(y)} h {ToInvariant(w)} v {ToInvariant(h)} h -{ToInvariant(w)} Z";

        var seriesArray = Series.ToArray();
        var dataList = data.ToList();

        var values = dataList
            .Select(h => seriesArray.Select(s => s.Selector(h)).ToArray())
            .ToList();

        var seriesGroups = new XElement[seriesArray.Length];

        for (int s = 0; s < seriesArray.Length; s++)
        {
            var spec = seriesArray[s];
            var paths = new List<XElement>();

            for (int hIndex = 0; hIndex < dataList.Count; hIndex++)
            {
                double value = values[hIndex][s];
                if (value <= 0) continue;

                double below = 0;
                for (int i = 0; i < s; i++)
                    below += values[hIndex][i];

                double heightPx = PlotHeight * value / maxStack;
                double y = PlotHeight - (PlotHeight * (below + value) / maxStack);
                double x = unitWidth * dataList[hIndex].Hour + barXStart;

                bool isTopMost = true;
                for (int i = s + 1; i < seriesArray.Length; i++)
                {
                    if (values[hIndex][i] > 0)
                    {
                        isTopMost = false;
                        break;
                    }
                }

                paths.Add(new XElement(svg + "path",
                    new XAttribute("d", isTopMost
                        ? RoundedTopPath(x, y, barWidth, heightPx)
                        : FlatRectPath(x, y, barWidth, heightPx)),
                    new XAttribute("fill", spec.Colour)));
            }

            seriesGroups[s] = new XElement(svg + "g", paths);
        }

        return new XElement(svg + "g",
            new XAttribute("clip-path", "url(#plot)"),
            new XAttribute("transform", $"translate({MarginLeft},{MarginTop})"),
            seriesGroups);
    }

    private static XElement CreateAverageLine(IEnumerable<HourlyEnergy> data,
                                              double maxStack,
                                              double unitWidth)
    {
        var sb = new StringBuilder("M ");

        foreach (var h in data.OrderBy(h => h.Hour))
        {
            double x = unitWidth * (h.Hour + 0.5);
            double y = PlotHeight - (PlotHeight * h.Consumption / maxStack);
            sb.Append(ToInvariant(x)).Append(',').Append(ToInvariant(y)).Append(' ');
        }

        var path = new XElement(svg + "path",
            new XAttribute("fill", "none"),
            new XAttribute("stroke", Color.AvgLine),
            new XAttribute("stroke-width", 2),
            new XAttribute("stroke-linejoin", "round"),
            new XAttribute("stroke-linecap", "round"),
            new XAttribute("d", sb.ToString().TrimEnd()));

        return new XElement(svg + "g",
            new XAttribute("clip-path", "url(#plot)"),
            new XAttribute("transform", $"translate({MarginLeft},{MarginTop})"),
            path);
    }

    private static XElement CreateXAxisLabels(double unitWidth)
    {
        var labels = Enumerable.Range(0, 24).Select(h =>
        {
            double x = MarginLeft + unitWidth * (h + .5);
            return new XElement(svg + "text",
                new XAttribute("x", ToInvariant(x)),
                new XAttribute("y", MarginTop + PlotHeight + 27),
                new XAttribute("text-anchor", "middle"),
                new XAttribute("style", $"font-size:12px;fill:{Color.AxisText};"),
                h.ToString("D2"));
        });
        return new XElement(svg + "g", labels);
    }

    private static XElement CreateLegend(Language language)
    {
        var items = new List<XElement>();

        // column-series items
        int offset = 0;
        foreach (var spec in Series)
        {
            items.Add(LegendItem(offset, spec.Colour, spec.LocalizedLabel(language), isLine: false));
            offset += 120;
        }
        // average line
        items.Add(LegendItem(
            offset,
            Color.AvgLine,
            language == Language.English ? "Annual consumption" : "Årligt forbrug",
            isLine: true));

        return new XElement(svg + "g",
            new XAttribute("transform", $"translate({LegendLeft},{LegendTop})"),
            new XElement(svg + "rect",
                new XAttribute("fill", "none"),
                new XAttribute("x", 0), new XAttribute("y", 0),
                new XAttribute("width", 517), new XAttribute("height", 30)),
            items);
    }

    private static XElement LegendItem(int xOffset, string colour, string label, bool isLine)
    {
        var group = new XElement(svg + "g",
            new XAttribute("transform", $"translate({xOffset},3)"));

        if (isLine)
        {
            group.Add(new XElement(svg + "path",
                new XAttribute("d", "M 1 13 L 15 13"),
                new XAttribute("fill", "none"),
                new XAttribute("stroke", colour),
                new XAttribute("stroke-width", 2),
                new XAttribute("stroke-linecap", "round")));
        }
        else
        {
            group.Add(new XElement(svg + "rect",
                new XAttribute("x", 2), new XAttribute("y", 6),
                new XAttribute("width", 12), new XAttribute("height", 12),
                new XAttribute("rx", 6), new XAttribute("ry", 6),
                new XAttribute("fill", colour)));
        }

        group.Add(new XElement(svg + "text",
            new XAttribute("x", 21), new XAttribute("y", 17),
            new XAttribute("style", "font-size:12px;font-family:OpenSansNormal;fill:#333;"),
            label));

        return group;
    }
}
