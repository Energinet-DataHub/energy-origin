using System;
using System.Net;

namespace EnergyOrigin.Setup.Exceptions;

public class BusinessException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public BusinessException(string msg) : base(msg)
    {
        StatusCode = HttpStatusCode.BadRequest;
    }

    public BusinessException(string msg, Exception innerException) : base(msg, innerException)
    {
        StatusCode = HttpStatusCode.BadRequest;
    }

    protected BusinessException(string msg, HttpStatusCode statusCode) : base(msg)
    {
        StatusCode = statusCode;
    }
}
