using System;

namespace API.ValueObjects;

public class IdpUserId : ValueObject
{
    public Guid Value { get; }

    public IdpUserId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be an empty Guid.", nameof(value));

        Value = value;
    }
}
