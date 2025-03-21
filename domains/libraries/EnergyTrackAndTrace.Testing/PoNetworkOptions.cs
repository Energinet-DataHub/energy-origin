using System.Collections.Generic;

namespace EnergyTrackAndTrace.Testing;

public record NetworkOptions
{
    public IDictionary<string, RegistryInfo> Registries { get; init; } = new Dictionary<string, RegistryInfo>();
    public IDictionary<string, AreaInfo> Areas { get; init; } = new Dictionary<string, AreaInfo>();
    public IDictionary<string, IssuerInfo> Issuers { get; init; } = new Dictionary<string, IssuerInfo>();

    public int? DaysBeforeCertificatesExpire { get; init; }

}

public record RegistryInfo
{
    public required string Url { get; init; }
}

public class AreaInfo
{
    public required IList<KeyInfo> IssuerKeys { get; set; }
}

public record KeyInfo
{
    public required string PublicKey { get; init; }
}

public record IssuerInfo
{
    public required string StampUrl { get; init; }
}
