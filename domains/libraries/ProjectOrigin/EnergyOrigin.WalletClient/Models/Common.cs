namespace EnergyOrigin.WalletClient.Models;

public record ResultList<T>()
{
    public required IEnumerable<T> Result { get; init; }
    public required PageInfo Metadata { get; init; }
}

public record PageInfo()
{
    public required int Count { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required int Total { get; init; }
}

public record FederatedStreamId()
{
    public required string Registry { get; init; }
    public required Guid StreamId { get; init; }
}

public enum CertificateType
{
    Consumption = 1,
    Production = 2
}
