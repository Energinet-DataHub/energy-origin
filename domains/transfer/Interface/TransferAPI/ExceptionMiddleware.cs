using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Transfer.Application.Exceptions;

namespace API;

public class ExceptionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ProblemDetailsFactory problemDetailsFactory;

    public ExceptionMiddleware(RequestDelegate next, ProblemDetailsFactory problemDetailsFactory)
    {
        this.next = next;
        this.problemDetailsFactory = problemDetailsFactory;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            var problemDetails =
                problemDetailsFactory.CreateValidationProblemDetails(context, new ModelStateDictionary(), StatusCodes.Status409Conflict,
                    detail: ex.Message);

            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
