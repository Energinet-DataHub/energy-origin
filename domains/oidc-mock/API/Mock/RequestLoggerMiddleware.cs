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

        logger.LogDebug("Request - (Scheme: {Scheme} Host: {Host}) {Method} (PathBase: {PathBase}) {Location}", req.Scheme, req.Host, req.Method, req.PathBase, $"{req.Path}{req.QueryString}".Sanitize());
        await next(httpContext);
        logger.LogDebug("Response - {StatusCode}", httpContext.Response.StatusCode);
    }
}

public static class StringLoggerExtensions {

    public static string Sanitize(this string input)
    {
        if (String.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        return input
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(Environment.NewLine, "");
    }
}
