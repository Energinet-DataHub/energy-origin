using System;

namespace EnergyOrigin.Setup.Exceptions;

public class NotFoundException : Exception
{
    protected NotFoundException(string str) : base(str)
    {

    }

    protected NotFoundException(string str, Exception innerException) : base(str, innerException)
    {

    }
}
