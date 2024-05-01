using System;

namespace API.ValueObjects;

public class Name : ValueObject
{
    public string Value { get; }

    private Name(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

        Value = value;
    }

    public static Name Create(string value)
    {
        return new Name(value);
    }
}
