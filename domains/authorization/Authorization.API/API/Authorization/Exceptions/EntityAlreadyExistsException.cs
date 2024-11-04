using System;

namespace API.Authorization.Exceptions;

public class EntityAlreadyExistsException : AlreadyExistsException
{
    public EntityAlreadyExistsException(string entityType) : base($"Entity of type {entityType} already exists.")
    {
    }

    public EntityAlreadyExistsException(Type entityType) : base($"Entity of type {entityType.Name} already exists.")
    {
    }
}
