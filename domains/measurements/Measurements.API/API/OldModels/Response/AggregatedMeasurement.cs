namespace API.OldModels.Response;

public record AggregatedMeasurement(
    long DateFrom,
    long DateTo,
    long Value
    );

