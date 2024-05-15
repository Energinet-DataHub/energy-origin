using System;

namespace API.Authorization.Exceptions;

public class NotFoundException : Exception
{
    protected NotFoundException(string str) : base(str)
    {

    }
}
