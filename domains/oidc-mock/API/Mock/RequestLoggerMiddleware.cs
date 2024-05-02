using System.Web;

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

        var scheme = HttpUtility.HtmlEncode(req.Scheme);
        var host = HttpUtility.HtmlEncode(req.Host.ToString());
        var method = HttpUtility.HtmlEncode(req.Method);
        var pathBase = HttpUtility.HtmlEncode(req.PathBase.ToString());
        var location = HttpUtility.HtmlEncode($"{req.Path}{req.QueryString}".Replace(Environment.NewLine, string.Empty));

        logger.LogDebug("Request - (Scheme: {Scheme} Host: {Host}) {Method} (PathBase: {PathBase}) {Location}", scheme, host, method, pathBase, location);

        await next(httpContext);

        var statusCode = httpContext.Response.StatusCode;
        logger.LogDebug("Response - {StatusCode}", statusCode);
    }
}
