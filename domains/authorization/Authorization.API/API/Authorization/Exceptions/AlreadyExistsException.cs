using System;

namespace API.Authorization.Exceptions;

public class AlreadyExistsException : Exception
{
    protected AlreadyExistsException(string str) : base(str)
    {
    }
}
