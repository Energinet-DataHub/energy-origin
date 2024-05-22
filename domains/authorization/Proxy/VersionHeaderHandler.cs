namespace Proxy
{
    public class VersionHeaderHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.TryGetValues("EO_API_VERSION", out var versions);
            var version = versions?.FirstOrDefault();

            if (version == "20250101")
            {
                request.Headers.Remove("EO_API_VERSION");
                var newUri = request.RequestUri?.ToString().Replace("http://localhost:5000/", "http://localhost:5000/v1/")
                             ?? throw new InvalidOperationException("RequestUri is null");
                request.RequestUri = new Uri(newUri);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
