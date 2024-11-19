using System;
using EnergyOrigin.Domain.ValueObjects;

namespace API.ValueObjects;

public class IdpId : ValueObject
{
    public Guid Value { get; }

    private IdpId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be an empty Guid.", nameof(value));

        Value = value;
    }

    public static IdpId Create(Guid value)
    {
        return new IdpId(value);
    }
}
