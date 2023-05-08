namespace DomainCertificate.ValueObjects;

public class Gsrn : ValueObject
{
    public string Value { get; }

    public Gsrn(string value)
    {
        //TODO: GSRN validation. 18 chars?

        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
