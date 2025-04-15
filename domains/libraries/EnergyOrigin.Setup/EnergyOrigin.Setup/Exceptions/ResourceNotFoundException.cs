using System;

namespace EnergyOrigin.Setup.Exceptions;

public class ResourceNotFoundException : NotFoundException
{
    public ResourceNotFoundException(string id)
        : base($"Resource with id {id} could not be found.")
    {
    }

    public ResourceNotFoundException(string id, Exception innerException)
        : base($"Resource with id {id} could not be found.", innerException)
    {
    }
}
