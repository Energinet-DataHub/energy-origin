using System.Text.RegularExpressions;

namespace EnergyOrigin.Domain.ValueObjects;

public partial class Gsrn : ValueObject
{
    [GeneratedRegex("^57\\d{16}$")]
    private static partial Regex MyRegex();
    private static readonly Regex regex = MyRegex();

    public string Value { get; }

    public Gsrn(string value)
    {
        if (!regex.IsMatch(value) || !value.StartsWith("57"))
            throw new ArgumentException("GSRN must be 18 characters long and start with 57. value: " + value);

        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}
