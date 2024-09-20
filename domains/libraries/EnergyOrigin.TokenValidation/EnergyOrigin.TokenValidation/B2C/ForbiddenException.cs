using System;

namespace EnergyOrigin.TokenValidation.b2c;

public class ForbiddenException : Exception
{
    public ForbiddenException(string msg) : base(msg)
    {

    }

    public ForbiddenException(Guid orgId) : base($"Not authorized to access organization with id {orgId}")
    {

    }
}
