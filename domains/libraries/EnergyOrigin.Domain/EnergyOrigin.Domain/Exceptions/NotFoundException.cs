namespace EnergyOrigin.Domain.Exceptions;

public class NotFoundException : Exception
{
    protected NotFoundException(string str) : base(str)
    {
    }
}
