namespace API.ReportGenerator.Domain;

public record DataPoint(int HourOfDay, double Value);
public record EnergySvgResult(string Svg);

public record HourlyEnergy
(
    int Hour,
    double Consumption, //rød linje
    double Matched, //grøn
    double Unmatched, //grå
    double Overmatched //blå
);

public record MunicipalityDistribution(string? Municipality, double Percentage);
