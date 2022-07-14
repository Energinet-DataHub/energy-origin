namespace Mock.Oidc;

// TODO: This is to be removed when the mock is completed
public class LogRequestMiddleware
{
    private readonly RequestDelegate _next;

    public LogRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ILogger<LogRequestMiddleware> logger)
    {
        await _next(httpContext);

        var req = httpContext.Request;
        var res = httpContext.Response;
        
        var message = $"{res.StatusCode} - {req.Method} (PathBase: {req.PathBase}) {req.Path}{req.QueryString}";

        if (res.StatusCode >= 400)
        {
            logger.LogWarning(message);
        }
        else
        {
            logger.LogInformation(message);
        }
    }
}