using System;

namespace API.ReportGenerator.Domain;

public sealed record DataPoint(DateTime Timestamp, double Value);
public record EnergySvgResult(string Svg, Metrics Metrics);

public sealed record HourlyEnergy
(
    int Hour,
    double Consumption,
    double Matched,
    double Unmatched,
    double Overmatched
);

public sealed record Metrics
(
    double Hourly,
    double Daily,
    double Weekly,
    double Monthly,
    double Annual
);
