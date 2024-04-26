using System;
using System.Text.RegularExpressions;

namespace API.ValueObjects;

public class OrganizationName : ValueObject
{
    public string Value { get; }

    // public static OrganizationName Create(string value) => new OrganizationName(value);
    // public static OrganizationName Empty() => new OrganizationName();
    // private OrganizationName() => Value = string.Empty;

    public OrganizationName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("OrganizationName cannot exceed 100 characters.", nameof(value));

        if (!IsValidOrganizationName(value))
            throw new ArgumentException("Value contains invalid characters.", nameof(value));

        Value = value.Trim();
    }

    private static bool IsValidOrganizationName(string value) => MyRegex().IsMatch(value);

    private static Regex MyRegex() => new Regex(@"^[a-zA-Z0-9\s&.,'\-]+$");

}
