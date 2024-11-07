namespace EnergyOrigin.Domain.ValueObjects.Tests;

public static class Any
{
    public static Guid Guid()
    {
        return System.Guid.NewGuid();
    }

    public static OrganizationId OrganizationId()
    {
        return ValueObjects.OrganizationId.Create(Guid());
    }
}
