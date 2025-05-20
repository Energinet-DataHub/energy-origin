using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Report
{
    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class ProcessedData
    {
        public int Hour { get; set; }
        public double Consumption { get; set; }
        public double Production { get; set; }
        public double Matched { get; set; }
        public double Unmatched { get; set; }
        public double Overmatched { get; set; }
    }

    public class Metrics
    {
        public double Hourly { get; set; }
        public double Daily { get; set; }
        public double Weekly { get; set; }
        public double Monthly { get; set; }
        public double Annual { get; set; }
    }

    public class EnergySvgResult
    {
        public required string Svg { get; set; }
        public required Metrics Metrics { get; set; }
    }

    public static class EnergyChartGenerator
    {
        private const int SVG_WIDTH = 754;
        private const int SVG_HEIGHT = 400;
        private static readonly Margin CHART_MARGIN = new Margin(top: 10, right: 10, bottom: 86, left: 10);

        private static class COLORS
        {
            public const string Unmatched = "#DFDFDF";
            public const string Overmatched = "#59ACE8";
            public const string Matched = "#82CF76";
            public const string AverageLine = "#FF0000";
            public const string Text = "#002433";
            public const string Background = "#F9FAFB";
            public const string HourText = "rgb(194,194,194)";
        }

        private struct Margin
        {
            public int Top, Right, Bottom, Left;
            public Margin(int top, int right, int bottom, int left)
            {
                Top = top;
                Right = right;
                Bottom = bottom;
                Left = left;
            }
        }

        public static EnergySvgResult GenerateEnergySvg(
            List<DataPoint> consumptionData,
            List<DataPoint> productionData,
            double hourlyCoverage = 50,
            double dailyCoverage = 77,
            double weeklyCoverage = 88,
            double monthlyCoverage = 95,
            double annualCoverage = 100,
            string period = "ÅRET 2024"
        )
        {
            // Process the data
            var processedData = ProcessData(consumptionData, productionData);

            // Calculate chart dimensions
            var chartWidth = SVG_WIDTH - CHART_MARGIN.Left - CHART_MARGIN.Right;
            var chartHeight = SVG_HEIGHT - CHART_MARGIN.Top - CHART_MARGIN.Bottom;

            // Generate the SVG elements
            var xAxis = GenerateXAxis(chartWidth, chartHeight);
            var gridLines = GenerateGridLines(chartWidth, chartHeight);
            var bars = GenerateBars(processedData, chartWidth, chartHeight);
            var averageLine = GenerateAverageLine(processedData, chartWidth, chartHeight);
            var legend = GenerateLegend(chartWidth);

            // Combine all elements into a single SVG
            var svgBuilder = new StringBuilder();
            svgBuilder.AppendLine($"<svg version=\"1.1\" class=\"highcharts-root\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {SVG_WIDTH} {SVG_HEIGHT}\" preserveAspectRatio=\"xMidYMid meet\" style=\"width: 100%; height: auto; display: block;\" role=\"img\" aria-label=\"\">");
            svgBuilder.AppendLine("  <defs>");
            svgBuilder.AppendLine("    <filter id=\"drop-shadow\"> <feDropShadow dx=\"1\" dy=\"1\" flood-color=\"#000000\" flood-opacity=\"0.75\" stdDeviation=\"2.5\"></feDropShadow> </filter>");
            svgBuilder.AppendLine("    <clipPath id=\"chart-area\"> <rect x=\"0\" y=\"0\" width=\"" + chartWidth + "\" height=\"" + chartHeight + "\" fill=\"none\"></rect> </clipPath>");
            svgBuilder.AppendLine("  </defs>");
            svgBuilder.AppendLine($"  <rect fill=\"{COLORS.Background}\" class=\"highcharts-background\" x=\"0\" y=\"0\" width=\"{SVG_WIDTH}\" height=\"{SVG_HEIGHT}\" rx=\"0\" ry=\"0\"></rect>");
            svgBuilder.AppendLine($"  <rect fill=\"none\" class=\"highcharts-plot-background\" x=\"{CHART_MARGIN.Left}\" y=\"{CHART_MARGIN.Top}\" width=\"{chartWidth}\" height=\"{chartHeight}\"></rect>");
            svgBuilder.AppendLine("  <!-- Grid lines -->");
            svgBuilder.AppendLine("  <g class=\"highcharts-grid highcharts-xaxis-grid\" data-z-index=\"1\">" + gridLines + "</g>");
            svgBuilder.AppendLine("  <!-- X axis -->");
            svgBuilder.AppendLine($"  <g class=\"highcharts-axis highcharts-xaxis\" data-z-index=\"2\"> <path fill=\"none\" class=\"highcharts-axis-line\" stroke=\"#ffffff\" stroke-width=\"1\" d=\"M {CHART_MARGIN.Left} {CHART_MARGIN.Top + chartHeight + 0.5} L {CHART_MARGIN.Left + chartWidth} {CHART_MARGIN.Top + chartHeight + 0.5}\"></path> </g>");
            svgBuilder.AppendLine("  <!-- Series group -->");
            svgBuilder.AppendLine($"  <g class=\"highcharts-series-group\" data-z-index=\"3\" transform=\"translate({CHART_MARGIN.Left},{CHART_MARGIN.Top})\">");
            svgBuilder.AppendLine("    <!-- Unmatched series -->");
            svgBuilder.AppendLine("    <g class=\"highcharts-series highcharts-series-0 highcharts-column-series\" data-z-index=\"0.1\" opacity=\"1\" clip-path=\"url(#chart-area)\">" + bars.Unmatched + "</g>");
            svgBuilder.AppendLine("    <!-- Overmatched series -->");
            svgBuilder.AppendLine("    <g class=\"highcharts-series highcharts-series-1 highcharts-column-series\" data-z-index=\"0.1\" opacity=\"1\" clip-path=\"url(#chart-area)\">" + bars.Overmatched + "</g>");
            svgBuilder.AppendLine("    <!-- Matched series -->");
            svgBuilder.AppendLine("    <g class=\"highcharts-series highcharts-series-2 highcharts-column-series\" data-z-index=\"0.1\" opacity=\"1\" clip-path=\"url(#chart-area)\">" + bars.Matched + "</g>");
            svgBuilder.AppendLine("    <!-- Average consumption line -->");
            svgBuilder.AppendLine("    <g class=\"highcharts-series highcharts-series-3 highcharts-line-series\" data-z-index=\"10\" opacity=\"1\" clip-path=\"url(#chart-area)\">" + averageLine + "</g>");
            svgBuilder.AppendLine("  </g>");
            svgBuilder.AppendLine("  <!-- X axis labels -->");
            svgBuilder.AppendLine("  <g class=\"highcharts-axis-labels highcharts-xaxis-labels\" data-z-index=\"7\">" + xAxis + "</g>");
            svgBuilder.AppendLine("  <!-- Legend -->");
            svgBuilder.AppendLine($"  <g class=\"highcharts-legend\" data-z-index=\"7\" transform=\"translate(119,355)\">{legend}</g>");
            svgBuilder.AppendLine("</svg>");

            return new EnergySvgResult
            {
                Svg = svgBuilder.ToString(),
                Metrics = new Metrics
                {
                    Hourly = hourlyCoverage,
                    Daily = dailyCoverage,
                    Weekly = weeklyCoverage,
                    Monthly = monthlyCoverage,
                    Annual = annualCoverage
                }
            };
        }

        private static List<ProcessedData> ProcessData(List<DataPoint> consumptionData, List<DataPoint> productionData)
        {
            if (consumptionData == null || productionData == null)
                throw new ArgumentException("Missing or invalid consumption/production data");

            var consumptionBuckets = new List<double>[24];
            var productionBuckets = new List<double>[24];
            for (int i = 0; i < 24; i++)
            {
                consumptionBuckets[i] = new List<double>();
                productionBuckets[i] = new List<double>();
            }

            // Group by UTC hour
            foreach (var dp in consumptionData)
            {
                var hour = dp.Timestamp.ToUniversalTime().Hour;
                consumptionBuckets[hour].Add(dp.Value);
            }
            foreach (var dp in productionData)
            {
                var hour = dp.Timestamp.ToUniversalTime().Hour;
                productionBuckets[hour].Add(dp.Value);
            }

            var result = new List<ProcessedData>(24);
            for (int hour = 0; hour < 24; hour++)
            {
                var avgConsumption = consumptionBuckets[hour].Any()
                    ? consumptionBuckets[hour].Average()
                    : 0.0;
                var avgProduction = productionBuckets[hour].Any()
                    ? productionBuckets[hour].Average()
                    : 0.0;
                var matched = Math.Min(avgConsumption, avgProduction);
                var unmatched = Math.Max(0, avgConsumption - avgProduction);
                var overmatched = Math.Max(0, avgProduction - avgConsumption);

                result.Add(new ProcessedData
                {
                    Hour = hour,
                    Consumption = avgConsumption,
                    Production = avgProduction,
                    Matched = matched,
                    Unmatched = unmatched,
                    Overmatched = overmatched
                });
            }

            return result;
        }

        private static string GenerateGridLines(int width, int height)
        {
            var sb = new StringBuilder();
            var hourWidth = (double)width / 24;
            for (int hour = 0; hour <= 24; hour++)
            {
                var x = CHART_MARGIN.Left + hour * hourWidth;
                var xPos = x + 0.5;
                sb.Append($"<path fill=\"none\" stroke=\"#e6e6e6\" stroke-width=\"0\" d=\"M {xPos} {CHART_MARGIN.Top} L {xPos} {CHART_MARGIN.Top + height}\" opacity=\"1\"></path>");
            }
            return sb.ToString();
        }

        private static string GenerateXAxis(int width, int height)
        {
            var sb = new StringBuilder();
            var hourWidth = (double)width / 24;
            for (int hour = 0; hour < 24; hour++)
            {
                var x = CHART_MARGIN.Left + hour * hourWidth + (hourWidth / 2);
                var label = hour.ToString("D2");
                sb.Append($"<text x=\"{x}\" style=\"cursor: default; font-size: 12px; fill: {COLORS.HourText};\" text-anchor=\"middle\" y=\"{CHART_MARGIN.Top + height + 27}\" opacity=\"1\">{label}</text>");
            }
            return sb.ToString();
        }

        private class Bars
        {
            public required string Matched { get; set; }
            public required string Unmatched { get; set; }
            public required string Overmatched { get; set; }
        }

        private static Bars GenerateBars(List<ProcessedData> data, int width, int height)
        {
            var hourWidth = (double)width / 24;
            var barWidth = hourWidth * 0.8;
            var barPadding = hourWidth * 0.1;

            // Determine scale max
            var maxValue = data.Select(d => Math.Max(d.Consumption, d.Matched + d.Unmatched + d.Overmatched)).Max();

            var sbMatched = new StringBuilder();
            var sbUnmatched = new StringBuilder();
            var sbOvermatched = new StringBuilder();

            for (int i = 0; i < data.Count; i++)
            {
                var d = data[i];
                var x = i * hourWidth + barPadding;
                var matchedHeight = height * (d.Matched / maxValue);
                var unmatchedHeight = height * (d.Unmatched / maxValue);
                var overmatchedHeight = height * (d.Overmatched / maxValue);

                var yMatched = height - matchedHeight;
                var yUnmatched = yMatched - unmatchedHeight;
                var yOvermatched = yUnmatched - overmatchedHeight;

                if (d.Matched > 0)
                    sbMatched.Append($"<rect x=\"{x}\" y=\"{yMatched}\" width=\"{barWidth}\" height=\"{matchedHeight}\" fill=\"{COLORS.Matched}\" />");
                if (d.Unmatched > 0)
                    sbUnmatched.Append($"<rect x=\"{x}\" y=\"{yUnmatched}\" width=\"{barWidth}\" height=\"{unmatchedHeight}\" fill=\"{COLORS.Unmatched}\" />");
                if (d.Overmatched > 0)
                    sbOvermatched.Append($"<rect x=\"{x}\" y=\"{yOvermatched}\" width=\"{barWidth}\" height=\"{overmatchedHeight}\" fill=\"{COLORS.Overmatched}\" />");
            }

            return new Bars
            {
                Matched = sbMatched.ToString(),
                Unmatched = sbUnmatched.ToString(),
                Overmatched = sbOvermatched.ToString()
            };
        }

        private static string GenerateAverageLine(List<ProcessedData> data, int width, int height)
        {
            var hourWidth = (double)width / 24;
            var maxValue = data.Select(d => Math.Max(d.Consumption, d.Matched + d.Unmatched + d.Overmatched)).Max();

            // Scale function
            Func<double, double> scaleY = value => height * (1 - (value / maxValue));
            var points = string.Join(" ", data.Select((d, i) =>
                (i * hourWidth + hourWidth / 2).ToString() + "," + scaleY(d.Consumption)));

            return $"<path fill=\"none\" d=\"M {points}\" class=\"highcharts-graph\" stroke=\"{COLORS.AverageLine}\" stroke-width=\"2\" stroke-linejoin=\"round\" stroke-linecap=\"round\"></path>";
        }

        private static string GenerateLegend(int width)
        {
            var items = new[]
            {
                new { Color = COLORS.Unmatched, Label = "Ikke matchet", IsLine = false },
                new { Color = COLORS.Overmatched, Label = "Overmatchet", IsLine = false },
                new { Color = COLORS.Matched, Label = "Matchet", IsLine = false },
                new { Color = COLORS.AverageLine, Label = "Yearly Average Consumption", IsLine = true }
            };

            var sb = new StringBuilder();
            sb.Append("<rect fill=\"none\" rx=\"0\" ry=\"0\" stroke=\"#999999\" stroke-width=\"0\" x=\"0\" y=\"0\" width=\"517\" height=\"30\"></rect><g>");
            int xOffset = 8;

            foreach (var item in items)
            {
                if (item.IsLine)
                {
                    sb.Append($"<g transform=\"translate({xOffset},3)\"><path fill=\"none\" d=\"M 1 13 L 15 13\" stroke=\"{item.Color}\" stroke-width=\"2\" stroke-linecap=\"round\"></path><text x=\"21\" y=\"17\" text-anchor=\"start\" style=\"font-size: 12px; fill: rgb(51, 51, 51);\">{item.Label}</text></g>");
                }
                else
                {
                    sb.Append($"<g transform=\"translate({xOffset},3)\"><rect x=\"2\" y=\"6\" rx=\"6\" ry=\"6\" width=\"12\" height=\"12\" fill=\"{item.Color}\"></rect><text x=\"21\" y=\"17\" text-anchor=\"start\" style=\"font-size: 12px; fill: rgb(51, 51, 51);\">{item.Label}</text></g>");
                }
                xOffset += item.Label.Length * 7 + 35;
            }

            sb.Append("</g>");
            return sb.ToString();
        }
    }
}
