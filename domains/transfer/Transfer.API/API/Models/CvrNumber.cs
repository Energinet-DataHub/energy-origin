using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace API.Models;

public class CvrNumber : ValueObject
{
    private const string Regexp = @"^\d{8}$";
    public string Value { get; }

    private static bool IsValid(string value)
    {
        var match = Regex.Match(value, Regexp, RegexOptions.IgnoreCase);
        return match.Success;
    }

    public static CvrNumber? TryParse(string value)
    {
        if(!IsValid(value))
            return null;

        return new CvrNumber(value);
    }

    public CvrNumber(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException($"Invalid CVR number: {value}.");

        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}

