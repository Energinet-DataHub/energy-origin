using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Proxy.Controllers;

public record GetClaimsQueryParametersCursor
{
    /// <summary>
    /// The start of the time range in Unix time in seconds.
    /// </summary>
    public long? Start { get; init; }

    /// <summary>
    /// The end of the time range in Unix time in seconds.
    /// </summary>
    public long? End { get; init; }

    /// <summary>
    /// Filter the type of certificates to return.
    /// </summary>
    public CertificateType? Type { get; init; }

    /// <summary>
    /// The number of items to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The time of the last update in Unix time in seconds.
    /// </summary>
    public long? UpdatedSince { get; init; }
}

public record GetCertificatesQueryParametersCursor
{
    /// <summary>
    /// The start of the time range in Unix time in seconds.
    /// </summary>
    public long? Start { get; init; }

    /// <summary>
    /// The end of the time range in Unix time in seconds.
    /// </summary>
    public long? End { get; init; }

    /// <summary>
    /// Filter the type of certificates to return.
    /// </summary>
    public CertificateType? Type { get; init; }

    /// <summary>
    /// The number of items to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The time of the last update in Unix time in seconds.
    /// </summary>
    public long? UpdatedSince { get; init; }
}
public record GetTransfersQueryParametersCursor
{
    /// <summary>
    /// The start of the time range in Unix time in seconds.
    /// </summary>
    public long? Start { get; init; }

    /// <summary>
    /// The end of the time range in Unix time in seconds.
    /// </summary>
    public long? End { get; init; }

    /// <summary>
    /// The number of items to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The time of the last update in Unix time in seconds.
    /// </summary>
    public long? UpdatedSince { get; init; }
}

public record GetCertificatesQueryParameters
{
    /// <summary>
    /// The start of the time range in Unix time in seconds.
    /// </summary>
    public long? Start { get; init; }

    /// <summary>
    /// The end of the time range in Unix time in seconds.
    /// </summary>
    public long? End { get; init; }

    /// <summary>
    /// Filter the type of certificates to return.
    /// </summary>
    public CertificateType? Type { get; init; }

    /// <summary>
    /// The number of items to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The number of items to skip.
    /// </summary>
    [DefaultValue(0)]
    public int Skip { get; init; }
}

public record AggregateCertificatesQueryParameters
{
    /// <summary>
    /// The size of each bucket in the aggregation.
    /// </summary>
    public required TimeAggregate TimeAggregate { get; init; }

    /// <summary>
    /// The time zone. See https://en.wikipedia.org/wiki/List_of_tz_database_time_zones for a list of valid time zones.
    /// </summary>
    public required string TimeZone { get; init; }

    /// <summary>
    ///The start of the time range in Unix time in seconds.
    /// </summary>
    public long? Start { get; init; }

    /// <summary>
    /// The end of the time range in Unix time in seconds.
    /// </summary>
    public long? End { get; init; }

    /// <summary>
    /// Filter the type of certificates to return.
    /// </summary>
    public CertificateType? Type { get; init; }

    /// <summary>
    /// The number of items to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The number of items to skip.
    /// </summary>
    [DefaultValue(0)]
    public int Skip { get; init; }
}

/// <summary>
/// A certificate that is available to use in the wallet.
/// </summary>
public record GranularCertificate()
{
    /// <summary>
    /// The id of the certificate.
    /// </summary>
    public required FederatedStreamId FederatedStreamId { get; init; }

    /// <summary>
    /// The quantity available on the certificate.
    /// </summary>
    public required uint Quantity { get; init; }

    /// <summary>
    /// The start of the certificate.
    /// </summary>
    public required long Start { get; init; }

    /// <summary>
    /// The end of the certificate.
    /// </summary>
    public required long End { get; init; }

    /// <summary>
    /// The Grid Area of the certificate.
    /// </summary>
    public required string GridArea { get; init; }

    /// <summary>
    /// The type of certificate (production or consumption).
    /// </summary>
    public required CertificateType CertificateType { get; init; }

    /// <summary>
    /// The attributes of the certificate.
    /// </summary>
    public required Dictionary<string, string> Attributes { get; init; }
}


/// <summary>
/// A result of aggregated certificates that is available to use in the wallet.
/// </summary>
public record AggregatedCertificates()
{
    /// <summary>
    /// The start of the aggregated period.
    /// </summary>
    public required long Start { get; init; }

    /// <summary>
    /// The end of the aggregated period.
    /// </summary>
    public required long End { get; init; }

    /// <summary>
    /// The quantity of the aggregated certificates.
    /// </summary>
    public required long Quantity { get; init; }

    /// <summary>
    /// The type of the aggregated certificates.
    /// </summary>
    public required CertificateType Type { get; init; }
}


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

public enum CertificateType
{
    Consumption = 1,
    Production = 2
}

public record FederatedStreamId()
{
    public required string Registry { get; init; }
    public required Guid StreamId { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimeAggregate
{
    Actual = 0,
    Total = 1,
    Year = 2,
    Month = 3,
    Week = 4,
    Day = 5,
    Hour = 6,
    QuarterHour = 7,
}
