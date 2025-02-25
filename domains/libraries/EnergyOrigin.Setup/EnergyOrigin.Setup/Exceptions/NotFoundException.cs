using System;

namespace EnergyOrigin.Setup.Exceptions;

public class NotFoundException : Exception
{
    protected NotFoundException(string str) : base(str)
    {

    }
}
