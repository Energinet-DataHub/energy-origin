using System;

namespace API.Transfer.Api.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException() : base("Not authorized to perform action")
    {

    }
    protected ForbiddenException(string str) : base(str)
    {
    }
}
