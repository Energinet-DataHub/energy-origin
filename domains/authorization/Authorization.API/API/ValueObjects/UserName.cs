using System;
using EnergyOrigin.Domain.ValueObjects;

namespace API.ValueObjects;

public class UserName : ValueObject
{
    public string Value { get; }

    private UserName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

        Value = value;
    }

    public static UserName Create(string value)
    {
        return new UserName(value);
    }
}
