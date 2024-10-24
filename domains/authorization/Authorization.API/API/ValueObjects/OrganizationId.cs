using System;
using EnergyOrigin.Domain.ValueObjects;

namespace API.ValueObjects;

public class OrganizationId : ValueObject
{
    public Guid Value { get; }

    public OrganizationId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be an empty Guid.", nameof(value));

        Value = value;
    }

    public static OrganizationId Create(Guid value)
    {
        return new OrganizationId(value);
    }
}
