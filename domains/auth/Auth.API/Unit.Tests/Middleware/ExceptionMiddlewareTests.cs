using System.Net;
using API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Unit.Tests.Middleware;
public class ExceptionMiddlewareTests
{

    private readonly ILogger<ExceptionMiddleware> logger = Substitute.For<ILogger<ExceptionMiddleware>>();
    private readonly RequestDelegate next = Substitute.For<RequestDelegate>();
    private readonly HttpContext httpContext = new DefaultHttpContext();
    private ExceptionMiddleware exceptionMiddleware => new ExceptionMiddleware(next, logger);


    [Fact]
    public async Task HandleExceptionsAsync_ShouldNotLogErrorOrChangeStatusCode_WhenNextIsASuccess()
    {
        next.Invoke(httpContext).Returns(Task.CompletedTask);

        await exceptionMiddleware.InvokeAsync(httpContext);

        logger.Received(0);
        Assert.Equal((int)HttpStatusCode.OK, httpContext.Response.StatusCode);
    }
    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenInvokeNextFails()
    {
        var exception = new Exception("Something went wrong");
        next.Invoke(httpContext).Throws(exception);

        await exceptionMiddleware.InvokeAsync(httpContext);

        logger.Received(1).Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<object>(), Arg.Is(exception), Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleExceptionAsync_ShouldChangeResposnseStatusCodeToErrorCode500_WhenNextFails()
    {
        var exception = new Exception("Something went wrong");
        next.Invoke(httpContext).Throws(exception);

        await exceptionMiddleware.InvokeAsync(httpContext);

        Assert.Equal((int)HttpStatusCode.InternalServerError, httpContext.Response.StatusCode);
    }
}
