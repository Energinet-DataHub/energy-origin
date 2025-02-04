using System;

namespace API.Authorization.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException() : base("Not authorized to perform action")
    {

    }
    protected ForbiddenException(string str) : base(str)
    {
    }
}

public class ServiceProviderTermsNotAcceptedException()
    : ForbiddenException("Organization has not accepted the latest service provider terms.");
