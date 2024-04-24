using System;
using System.Linq;

namespace API.ValueObjects;

public readonly record struct Tin
{
    private string Value { get; }

    public Tin(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));
        }

        if (value.Length != 8 || !IsDigitsOnly(value))
        {
            throw new ArgumentException("Value must be exactly 8 digits.", nameof(value));
        }

        Value = value;
    }

    private static bool IsDigitsOnly(string str)
    {
        return str.All(char.IsDigit);
    }

    public override string ToString()
    {
        return Value;
    }
}
