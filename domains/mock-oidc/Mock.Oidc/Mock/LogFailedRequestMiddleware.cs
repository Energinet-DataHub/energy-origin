namespace Mock.Oidc;

// TODO: This is to be removed when the mock is completed
public class LogFailedRequestMiddleware
{
    private readonly RequestDelegate _next;

    public LogFailedRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ILogger<LogFailedRequestMiddleware> logger)
    {
        await _next(httpContext);

        var req = httpContext.Request;
        var res = httpContext.Response;
        if (res.StatusCode >= 400)
        {
            logger.LogWarning($"{res.StatusCode} - {req.Method} {req.Path}{req.QueryString}");
        }
    }
}