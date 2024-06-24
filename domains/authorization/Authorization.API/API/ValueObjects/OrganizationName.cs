using System;
using System.Text.RegularExpressions;

namespace API.ValueObjects;

public class OrganizationName : ValueObject
{
    public string Value { get; }

    public OrganizationName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

        Value = value.Trim();
    }

    public static OrganizationName Create(string value)
    {
        return new OrganizationName(value);
    }
}
