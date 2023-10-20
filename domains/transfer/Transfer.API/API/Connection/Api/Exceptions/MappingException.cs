using System;

namespace API.Connection.Api.Exceptions;

internal class MappingException : Exception
{
    public MappingException(string? message) : base(message)
    {
    }

    public MappingException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
