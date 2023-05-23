using System.Text.RegularExpressions;

namespace DomainCertificate.ValueObjects;

public class Gsrn : ValueObject
{
    private readonly Regex regex = new("^57\\d{16}$");
    public string Value { get; }

    public Gsrn(string value)
    {
        if (!regex.IsMatch(value) || !value.StartsWith("57"))
            throw new ArgumentException("GSRN must be 18 characters long and start with 57.");

        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
