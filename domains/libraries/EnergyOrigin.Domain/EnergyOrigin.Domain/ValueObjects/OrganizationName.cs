namespace EnergyOrigin.Domain.ValueObjects;

public class OrganizationName : ValueObject
{
    public string Value { get; }

    private OrganizationName()
    {
        Value = String.Empty;
    }

    private OrganizationName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));
        }

        Value = value.Trim();
    }

    public static OrganizationName Create(string value)
    {
        return new OrganizationName(value);
    }

    public static OrganizationName Empty()
    {
        return new OrganizationName();
    }

    public override string ToString()
    {
        return Value;
    }
}
