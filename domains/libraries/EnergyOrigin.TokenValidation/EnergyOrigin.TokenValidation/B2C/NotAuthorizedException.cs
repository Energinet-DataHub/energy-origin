using System;

namespace EnergyOrigin.TokenValidation.b2c;

public class NotAuthorizedException : Exception
{
    public NotAuthorizedException(string msg) : base(msg)
    {

    }

    public NotAuthorizedException(Guid orgId) : base($"Not authorized to access organization with id {orgId}")
    {

    }
}
