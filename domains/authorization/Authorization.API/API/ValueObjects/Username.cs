using System;

namespace API.ValueObjects;

public class Username : ValueObject
{
    public string Value { get; }

    private Username(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

        Value = value;
    }

    public static Username Create(string value)
    {
        return new Username(value);
    }
}
