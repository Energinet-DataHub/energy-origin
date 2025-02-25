using System;

namespace EnergyOrigin.Setup.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string msg) : base(msg)
    {

    }

    public ForbiddenException() : base("Not authorized to perform action")
    {

    }

    public ForbiddenException(Guid orgId) : base($"Not authorized to access organization with id {orgId}")
    {

    }
}
