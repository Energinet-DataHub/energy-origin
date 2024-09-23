using System.Web;

namespace Oidc.Mock
{
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

            // Sanitize all necessary parts of the request
            var sanitizedScheme = HttpUtility.UrlEncode(req.Scheme);
            var sanitizedHost = HttpUtility.UrlEncode(req.Host.Value);
            var sanitizedMethod = HttpUtility.UrlEncode(req.Method);
            var sanitizedPathBase = HttpUtility.UrlEncode(req.PathBase.Value);
            var sanitizedPath = HttpUtility.UrlEncode(req.Path.Value);
            var sanitizedQueryString = HttpUtility.UrlEncode(req.QueryString.Value);

            // Log the sanitized request data
            logger.LogDebug("Request - (Scheme: {Scheme} Host: {Host}) {Method} (PathBase: {PathBase}) {Location}",
                sanitizedScheme,
                sanitizedHost,
                sanitizedMethod,
                sanitizedPathBase,
                $"{sanitizedPath}{sanitizedQueryString}");

            await next(httpContext);

            // Log the response status code
            logger.LogDebug("Response - {StatusCode}", httpContext.Response.StatusCode);
        }
    }
}
