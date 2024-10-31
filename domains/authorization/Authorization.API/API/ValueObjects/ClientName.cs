using System;
using EnergyOrigin.Domain.ValueObjects;

namespace API.ValueObjects;

public class ClientName : ValueObject
{
    public string Value { get; }

    public ClientName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

        Value = value.Trim();
    }

    public static ClientName Create(string value)
    {
        return new ClientName(value);
    }
}
