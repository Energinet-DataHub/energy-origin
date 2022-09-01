namespace API.Shared.DataSync.Models;

public record Measurement(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    Quality Quality
    );
