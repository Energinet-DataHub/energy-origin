using System;
using EnergyOrigin.Domain.ValueObjects;

namespace API.Models;

public class ClientCredential
{
    private ClientCredential(string? hint, Guid keyId, DateTimeOffset? startDateTime, DateTimeOffset? endDateTime)
    {
        Hint = hint;
        KeyId = keyId;
        StartDateTime = startDateTime is null ? null : UnixTimestamp.Create((DateTimeOffset)startDateTime);
        EndDateTime = endDateTime is null ? null : UnixTimestamp.Create((DateTimeOffset)endDateTime);
    }

    private ClientCredential(string? hint, Guid keyId, DateTimeOffset? startDateTime, DateTimeOffset? endDateTime,
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

    public static ClientCredential Create(string? hint, Guid keyId, DateTimeOffset? startDateTime,
        DateTimeOffset? endDateTime)
    {
        return new ClientCredential(hint, keyId, startDateTime, endDateTime);
    }

    public static ClientCredential Create(string? hint, Guid keyId, DateTimeOffset? startDateTime,
        DateTimeOffset? endDateTime, string secret)
    {
        return new ClientCredential(hint, keyId, startDateTime, endDateTime, secret);
    }
}
