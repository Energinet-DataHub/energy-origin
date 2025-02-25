using System;

namespace EnergyOrigin.Setup.Exceptions;

public class EntityNotFoundException : NotFoundException
{
    public EntityNotFoundException(string id, string entityType) : base($"Entity of type {entityType} with id(s) {id} not found.")
    {
    }

    public EntityNotFoundException(Guid id, Type entityType) : base($"Entity of type {entityType.Name} with id(s) {id} not found.")
    {
    }
}
