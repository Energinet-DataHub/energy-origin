using System;
using System.Text.Json.Serialization;

namespace API.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TermsType
{
    Normal,
    Trial
}


public class Terms : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public int Version { get; private set; }
    public TermsType Type { get; private set; }

    private Terms() { }

    public static Terms Create(int version, TermsType type = TermsType.Normal) =>
        new()
        {
            Id = Guid.NewGuid(),
            Version = version,
            Type = type
        };
}
