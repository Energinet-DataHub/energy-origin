using System;
using System.Text.RegularExpressions;

namespace API.ValueObjects;

public readonly partial record struct OrganizationName
{
    private string Value { get; }

    public OrganizationName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));
        }

        if (value.Length > 100)
        {
            throw new ArgumentException("Value cannot exceed 100 characters.", nameof(value));
        }

        if (!IsValidOrganizationName(value))
        {
            throw new ArgumentException("Value contains invalid characters.", nameof(value));
        }

        Value = value.Trim();
    }

    private static bool IsValidOrganizationName(string value)
    {
        return MyRegex().IsMatch(value);
    }

    public override string ToString()
    {
        return Value;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\s&.,'\-]+$")]
    private static partial Regex MyRegex();
}
