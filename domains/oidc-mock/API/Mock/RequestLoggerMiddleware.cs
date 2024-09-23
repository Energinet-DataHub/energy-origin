namespace Oidc.Mock;

public class RequestLoggerMiddleware
{
    private readonly RequestDelegate next;

    public RequestLoggerMiddleware(RequestDelegate next) => this.next = next;

    public async Task InvokeAsync(HttpContext httpContext, ILogger<RequestLoggerMiddleware> logger)
    {
        var req = httpContext.Request;
        if (string.Equals(req.Path.Value, "/health", StringComparison.InvariantCultureIgnoreCase))
        {
            await next(httpContext);
            return;
        }

        var sanitizedPathBase = req.PathBase.Value!.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        var sanitizedPath = req.Path.Value!.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        var sanitizedQueryString = req.QueryString.Value!.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        var sanitizedScheme = req.Scheme.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        var sanitizedHost = req.Host.Value!.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        var sanitizedMethod = req.Method.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        logger.LogDebug("Request - (Scheme: {Scheme} Host: {Host}) {Method} (PathBase: {PathBase}) {Location}", sanitizedScheme, sanitizedHost, sanitizedMethod, sanitizedPathBase, $"{sanitizedPath}{sanitizedQueryString}");
        await next(httpContext);
        logger.LogDebug("Response - {StatusCode}", httpContext.Response.StatusCode);
    }
}
