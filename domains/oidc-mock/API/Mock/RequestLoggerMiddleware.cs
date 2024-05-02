namespace Oidc.Mock;

public class RequestLoggerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, ILogger<RequestLoggerMiddleware> logger)
    {
        var req = httpContext.Request;
        if (string.Equals(req.Path.Value, "/health", StringComparison.InvariantCultureIgnoreCase))
        {
            await next(httpContext);
            return;
        }

        var scheme = req.Scheme.Replace(Environment.NewLine, string.Empty);
        var host = req.Host.ToString().Replace(Environment.NewLine, string.Empty);
        var method = req.Method.Replace(Environment.NewLine, string.Empty);
        var pathBase = req.PathBase.ToString().Replace(Environment.NewLine, string.Empty);
        var location = $"{req.Path}{req.QueryString}".Replace(Environment.NewLine, string.Empty);

        logger.LogDebug("Request - (Scheme: {Scheme} Host: {Host}) {Method} (PathBase: {PathBase}) {Location}", scheme, host, method, pathBase, location);

        await next(httpContext);

        var statusCode = httpContext.Response.StatusCode;
        logger.LogDebug("Response - {StatusCode}", statusCode);
    }
}
