﻿using System;

namespace API.ValueObjects;

public class IdpClientId : ValueObject
{
    public Guid Value { get; }

    public IdpClientId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be an empty Guid.", nameof(value));

        Value = value;
    }
}
