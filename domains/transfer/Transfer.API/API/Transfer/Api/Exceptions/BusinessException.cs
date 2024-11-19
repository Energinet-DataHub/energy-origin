using System;
using System.Net;

namespace API.Transfer.Api.Exceptions;

public class BusinessException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public BusinessException(string msg) : base(msg)
    {
        StatusCode = HttpStatusCode.BadRequest;
    }

    protected BusinessException(string msg, HttpStatusCode statusCode) : base(msg)
    {
        StatusCode = statusCode;
    }
}