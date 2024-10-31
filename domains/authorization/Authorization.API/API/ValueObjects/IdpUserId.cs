using System;
using EnergyOrigin.Domain.ValueObjects;

namespace API.ValueObjects;

public class IdpUserId : ValueObject
{
    public Guid Value { get; }

    private IdpUserId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be an empty Guid.", nameof(value));

        Value = value;
    }

    public static IdpUserId Create(Guid value)
    {
        return new IdpUserId(value);
    }
}
