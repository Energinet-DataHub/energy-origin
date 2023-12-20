using System;

namespace RegistryConnector.Worker.Exceptions;

[Serializable]
public class TransientException : Exception
{
    public TransientException(string message, Exception ex) : base(message, ex)
    {
    }
}
