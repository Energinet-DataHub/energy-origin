namespace API.Models;

public record AggregatedMeasurement(
    long DateFrom,
    long DateTo,
    long Value
    );

