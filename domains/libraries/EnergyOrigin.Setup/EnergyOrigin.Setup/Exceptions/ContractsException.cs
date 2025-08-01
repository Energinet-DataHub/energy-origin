using System;
using System.Net;

namespace EnergyOrigin.Setup.Exceptions;

public class ContractsException(string msg, HttpStatusCode statusCode) : Exception(msg)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
