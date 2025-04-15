using System;
using EnergyOrigin.Domain.ValueObjects;

namespace API.Models;

public class ConfidentialClientCredetial
{
    private ConfidentialClientCredetial(string? hint, Guid keyId, DateTimeOffset? startDateTime,
        DateTimeOffset? endDateTime,
        string secret)
    {
        Hint = hint;
        KeyId = keyId;
        StartDateTime = startDateTime is null ? null : UnixTimestamp.Create((DateTimeOffset)startDateTime);
        EndDateTime = endDateTime is null ? null : UnixTimestamp.Create((DateTimeOffset)endDateTime);
        Secret = secret;
    }

    public string? Hint { get; set; }
    public Guid KeyId { get; set; }
    public UnixTimestamp? StartDateTime { get; set; }
    public UnixTimestamp? EndDateTime { get; set; }
    public string? Secret { get; set; }

    public static ConfidentialClientCredetial Create(string? hint, Guid keyId, DateTimeOffset? startDateTime,
        DateTimeOffset? endDateTime, string secret)
    {
        return new ConfidentialClientCredetial(hint, keyId, startDateTime, endDateTime, secret);
    }
}
