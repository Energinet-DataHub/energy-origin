using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EnergyReporting;

public record DataPoint(DateTime Timestamp, double Value);
public record Metrics(double Hourly, double Daily, double Weekly, double Monthly, double Annual);
public record EnergySvgResult(string Svg, Metrics Metrics);

public static class EnergyChartGenerator
{
    private const int SvgW = 754, SvgH = 400;
    private static readonly (int Top, int Left) M = (10, 10);
    private static readonly (string Unmatched, string Overmatched, string Matched, string AvgLine, string Bg, string HourText)
        C = ("#DFDFDF", "#59ACE8", "#82CF76", "#FF0000", "#F9FAFB", "rgb(194,194,194)");

    public static EnergySvgResult GenerateEnergySvg(
        IEnumerable<DataPoint> cons,
        IEnumerable<DataPoint> prod,
        Metrics? metrics = null) => ((Func<EnergySvgResult>)(() =>
    {
        metrics ??= new Metrics(50, 77, 88, 95, 100);
        var cw = SvgW - M.Left * 2;
        var ch = SvgH - M.Top - 86;
        var consL = cons.ToLookup(dp => dp.Timestamp.ToUniversalTime().Hour, dp => dp.Value);
        var prodL = prod.ToLookup(dp => dp.Timestamp.ToUniversalTime().Hour, dp => dp.Value);
        var hours = Enumerable.Range(0,24)
            .Select(h =>
            {
                var avgC = consL[h].DefaultIfEmpty().Average();
                var avgP = prodL[h].DefaultIfEmpty().Average();
                var m = Math.Min(avgC, avgP);
                return (h, avgC, unmatched: avgC-m, over: avgP-m, matched: m);
            })
            .ToArray();
        var maxV = hours.Max(t => Math.Max(t.avgC, t.unmatched + t.over + t.matched));
        var unitW = (double)cw/24;
        var yPos = (Func<double,double>)(v => M.Top + ch*(1-v/maxV));
        var svg = new XElement("svg",
            new XAttribute("viewBox",$"0 0 {SvgW} {SvgH}"),
            new XAttribute("xmlns","http://www.w3.org/2000/svg"),
            new XAttribute("style","width:100%;height:auto;display:block;"),
            new XElement("rect",new XAttribute("fill",C.Bg),new XAttribute("width",SvgW),new XAttribute("height",SvgH)),
            new XElement("g",Enumerable.Range(0,25).Select(i=>new XElement("line",
                new XAttribute("x1",M.Left+unitW*i),new XAttribute("y1",M.Top),
                new XAttribute("x2",M.Left+unitW*i),new XAttribute("y2",M.Top+ch),
                new XAttribute("stroke","#e6e6e6")))),
            new XElement("g", hours.SelectMany(t =>
            {
                var x0 = M.Left + unitW * t.h + unitW * 0.1;
                var y0Base = M.Top + ch;
                var layers = new (double Value, string Color)[]
                {
                    (t.matched, C.Matched),
                    (t.unmatched, C.Unmatched),
                    (t.over, C.Overmatched)
                };
                return layers
                    .Where(layer => layer.Value > 0)
                    .Select(layer =>
                    {
                        var hgt = ch * layer.Value / maxV;
                        var y0 = y0Base - hgt;
                        return new XElement("rect",
                            new XAttribute("x", x0),
                            new XAttribute("y", y0),
                            new XAttribute("width", unitW * 0.8),
                            new XAttribute("height", hgt),
                            new XAttribute("fill", layer.Color));
                    });
            })),
            new XElement("path",new XAttribute("d","M "+string.Join(' ',hours.Select(t=>$"{M.Left+unitW*(t.h+0.5)},{yPos(t.avgC)}"))),new XAttribute("stroke",C.AvgLine),new XAttribute("fill","none")),
            new XElement("g",hours.Select(t=>new XElement("text",
                new XAttribute("x",M.Left+unitW*(t.h+0.5)),new XAttribute("y",M.Top+ch+27),
                new XAttribute("text-anchor","middle"),new XAttribute("fill",C.HourText),t.h.ToString("D2")))),
            new XElement("g",new XAttribute("transform","translate(119,355)"),new[]{("Ikke matchet",C.Unmatched),("Overmatchet",C.Overmatched),("Matchet",C.Matched),("Avg",C.AvgLine)}.Select((it,idx)=>
                new XElement("g",new XAttribute("transform",$"translate({8+idx*80},3)"),
                    new XElement("rect",new XAttribute("x",0),new XAttribute("y",6),
                        new XAttribute("width",12),new XAttribute("height",12),new XAttribute("fill",it.Item2)),
                    new XElement("text",new XAttribute("x",18),new XAttribute("y",17),it.Item1))))
        );
        return new EnergySvgResult(svg.ToString(SaveOptions.DisableFormatting),metrics);
    }))();
}
