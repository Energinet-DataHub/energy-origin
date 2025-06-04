namespace API.ReportGenerator.Domain;

public record DataPoint(int HourOfDay, double Value);
public record EnergySvgResult(string Svg);

public record HourlyEnergy
(
    int Hour,
    double Consumption,
    double Matched,
    double Unmatched,
    double Overmatched
);

public record MunicipalityDistribution(string Municipality, double Percentage);
