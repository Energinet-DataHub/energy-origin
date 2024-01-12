using System;

namespace API.Shared.Exceptions;

public class DataHubFacadeException : SanitizedException
{
    public DataHubFacadeException(string message, Exception? innerException = null) :
        base(@$"{message}", innerException)
    {
    }
}
