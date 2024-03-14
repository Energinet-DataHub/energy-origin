using System.Runtime.Serialization;

namespace Transfer.Application.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
    {
    }

    protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ValidationException(string? message) : base(message)
    {
    }

    public ValidationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
