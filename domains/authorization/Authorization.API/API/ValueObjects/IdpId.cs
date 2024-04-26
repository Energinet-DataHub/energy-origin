using System;

namespace API.ValueObjects;

public class IdpId : ValueObject
{
    public Guid Value { get; }

    public IdpId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be an empty Guid.", nameof(value));

        Value = value;
    }
}
