namespace Oidc.Mock;

public class RequestLoggerMiddleware
{
    private readonly RequestDelegate next;

    public RequestLoggerMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ILogger<RequestLoggerMiddleware> logger)
    {
        var req = httpContext.Request;
        if (string.Equals(req.Path.Value, "/health", StringComparison.InvariantCultureIgnoreCase))
        {
            await next(httpContext);
            return;
        }

        logger.LogDebug($"Request - (Scheme: {req.Scheme} Host: {req.Host}) {req.Method} (PathBase: {req.PathBase}) {req.Path}{req.QueryString}");
        await next(httpContext);
        logger.LogDebug($"Response - {httpContext.Response.StatusCode}");
    }
}
