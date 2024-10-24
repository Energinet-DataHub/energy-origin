using System;

namespace API.Authorization.Exceptions;

public class BusinessException : Exception
{
    protected BusinessException(string msg) : base(msg)
    {

    }
}
