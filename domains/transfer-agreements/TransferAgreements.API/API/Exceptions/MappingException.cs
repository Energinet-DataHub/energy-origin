using System;

namespace API.Exceptions;

internal class MappingException : Exception
{
    public MappingException(string? message) : base(message)
    {
    }

    public MappingException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
