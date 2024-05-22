using System;
using System.Text.RegularExpressions;

namespace API.ValueObjects;

public class ClientName : ValueObject
{
    public string Value { get; }

    public ClientName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

        if (!IsValidClientName(value))
            throw new ArgumentException("Value contains invalid characters.", nameof(value));

        Value = value.Trim();
    }

    private static bool IsValidClientName(string value) => MyRegex().IsMatch(value);

    private static Regex MyRegex() => new Regex(@"^[a-zA-Z0-9\s&.,'/\-]+$");

    public static ClientName Create(string value)
    {
        return new ClientName(value);
    }
}
