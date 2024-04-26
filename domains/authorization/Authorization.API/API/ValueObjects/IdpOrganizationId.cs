using System;

namespace API.ValueObjects;

public class IdpOrganizationId : ValueObject
{
    public Guid Value { get; }

    public IdpOrganizationId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be an empty Guid.", nameof(value));

        Value = value;
    }
}
