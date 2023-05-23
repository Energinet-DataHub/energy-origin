namespace DomainCertificate.ValueObjects;

public class Gsrn : ValueObject
{
    public string Value { get; }

    public Gsrn(string value)
    {
        if (value.Length != 18 || !value.StartsWith("57"))
            throw new ArgumentException("GSRN must be 18 characters long and start with 57.");

        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
