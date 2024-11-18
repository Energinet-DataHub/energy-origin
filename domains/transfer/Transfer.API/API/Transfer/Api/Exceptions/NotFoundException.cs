using System;

namespace API.Transfer.Api.Exceptions;

public class NotFoundException : Exception
{
    protected NotFoundException(string str) : base(str)
    {
    }
}
