using System;

namespace API.Authorization.Exceptions;

public class ForbiddenException : Exception
{
    protected ForbiddenException(string str) : base(str)
    {
    }
}
