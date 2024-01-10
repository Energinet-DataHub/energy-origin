namespace API.Models.Response;

public record AggregatedMeasurement(
    long DateFrom,
    long DateTo,
    long Value
    );

