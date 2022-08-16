namespace API.Models;

//NOTE: This DTO is a reflection of the output from the DataSync API.
public record Measurement
{
    public string GSRN { get; init; }

    public long DateFrom { get; init; }

    public long DateTo { get; init; }

    public ulong Quantity { get; init; }

    public Quality Quality { get; init; }
}
