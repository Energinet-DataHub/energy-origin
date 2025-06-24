namespace EnergyOrigin.Domain.ValueObjects;

public class OrganizationId : ValueObject
{
    public Guid Value { get; private set; }

    public OrganizationId()
    {
        Value = Guid.Empty;
    }

    private OrganizationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"Value cannot be {Guid.Empty}", nameof(value));
        }

        Value = value;
    }

    public static OrganizationId Create(Guid value)
    {
        return new OrganizationId(value);
    }

    public static OrganizationId Empty()
    {
        return new OrganizationId();
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
