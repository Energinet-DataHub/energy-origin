namespace EnergyOrigin.TokenValidation.b2c;

public class ForbiddenException : Exception
{
    public ForbiddenException(Guid orgId) : base($"Not authorized to access organization with id {orgId}")
    {

    }
}
