namespace Mock.Oidc;

// TODO: This is to be removed when the mock is completed
public class RequestLoggerMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ILogger<RequestLoggerMiddleware> logger)
    {
        await _next(httpContext);

        var req = httpContext.Request;
        var res = httpContext.Response;

        if (string.Equals(req.Path.Value, "/health", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        var message = $"{res.StatusCode} - (Scheme: {req.Scheme} Host: {req.Host}) {req.Method} (PathBase: {req.PathBase}) {req.Path}{req.QueryString}";

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