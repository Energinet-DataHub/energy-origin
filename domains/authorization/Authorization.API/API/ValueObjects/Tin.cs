using System;
using System.Linq;

namespace API.ValueObjects;

public readonly record struct Tin : IComparable<Tin>
{
    public string Value { get; }

    public int CompareTo(Tin other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public Tin(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

        if (value.Length != 8 || !IsDigitsOnly(value))
            throw new ArgumentException("Value must be exactly 8 digits.", nameof(value));

        Value = value;
    }

    private static bool IsDigitsOnly(string str) => str.All(char.IsDigit);
}
