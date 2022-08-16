namespace API.Models;

//NOTE: This DTO is a reflection of the output from the DataSync API.
public record Measurement(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    Quality Quality
    );
