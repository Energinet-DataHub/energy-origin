namespace API.ReportGenerator.Domain;

public record DataPoint(int HourOfDay, double Value);
public record EnergySvgResult(string Svg);

public record HourlyEnergy
(
    int Hour,
    double Consumption, //red line
    double Matched, //green
    double Unmatched, //grey
    double Overmatched //blue
);

public record MunicipalityDistribution(string? Municipality, double Percentage);
