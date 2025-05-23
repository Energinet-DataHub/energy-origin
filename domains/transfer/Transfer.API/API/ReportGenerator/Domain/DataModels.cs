using System;

namespace API.ReportGenerator.Domain;

public sealed record DataPoint(DateTime Timestamp, double Value);
public record EnergySvgResult(string Svg);

public sealed record HourlyEnergy
(
    int Hour,
    double Consumption,
    double Matched,
    double Unmatched,
    double Overmatched
);
