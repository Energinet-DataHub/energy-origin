using System;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace API.Transfer.Api.Exceptions;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured.");
            var error = ex switch
            {
                NotFoundException e => new Error(HttpStatusCode.NotFound, e.Message),
                ForbiddenException e => new Error(HttpStatusCode.Forbidden, e.Message),
                BusinessException e => new Error(e.StatusCode, e.Message),
                _ => new Error(HttpStatusCode.InternalServerError, ex.Message)
            };

            context.Response.ContentType = MediaTypeNames.Application.ProblemJson;
            context.Response.StatusCode = (int)error.StatusCode;
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            var result = JsonSerializer.Serialize(error, options);
            await context.Response.WriteAsync(result, context.RequestAborted);
        }
    }
}

public class Error
{
    public string Status { get; private set; }
    public int StatusCode { get; private set; }
    public string Description { get; private set; }

    public Error(HttpStatusCode statusCode, string description)
    {
        Status = statusCode.ToString();
        StatusCode = (int)statusCode;
        Description = description;
    }
}
