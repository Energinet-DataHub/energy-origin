using System;
using System.Linq;

namespace API.ValueObjects;

public class Tin : ValueObject
{
    public string Value { get; }

    public Tin(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));
        if (value.Length != 8 || !IsDigitsOnly(value))
            throw new ArgumentException("Value must be exactly 8 digits.", nameof(value));

        Value = value;
    }

    private static bool IsDigitsOnly(string str) => str.All(char.IsDigit);

    public static Tin Create(string value)
    {
        return new Tin(value);
    }
}
