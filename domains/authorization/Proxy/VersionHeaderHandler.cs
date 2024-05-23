using System.Net;

namespace Proxy;

public class VersionHeaderHandler : DelegatingHandler
{
    private readonly string[] _excludedRoutes = [ ExcludedRoutes.SwaggerEndpoint ];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestUri = request.RequestUri?.ToString();

        if (requestUri is not null && IsExcludedRoute(requestUri))
        {
            return base.SendAsync(request, cancellationToken);
        }

        var hasVersionHeader = request.Headers.TryGetValues("EO_API_VERSION", out var versions);
        var version = versions?.FirstOrDefault();

        if (!hasVersionHeader || version != ApiVersions.Version20250101)
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Missing or invalid EO_API_VERSION header.")
            });

        if (requestUri is null || !requestUri.StartsWith("http://localhost:5000/wallet-api/", StringComparison.OrdinalIgnoreCase))
            return base.SendAsync(request, cancellationToken);

        var newUri = requestUri.Replace("http://localhost:5000/wallet-api/", "http://localhost:5000/wallet-api/v1/");
        request.RequestUri = new Uri(newUri);

        return base.SendAsync(request, cancellationToken);
    }

    private bool IsExcludedRoute(string requestUri)
    {
        return _excludedRoutes.Any(route => requestUri.Contains(route, StringComparison.OrdinalIgnoreCase));
    }
}
