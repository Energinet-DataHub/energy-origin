using System.Net;
using Polly.Retry;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using Polly;

namespace API.MeasurementsSyncer.Clients.DataHub3;

public class AuthHeaderHandler : DelegatingHandler
{
    readonly ITokenService _tokenProvider;
    readonly AsyncRetryPolicy<HttpResponseMessage> _policy;

    public AuthHeaderHandler(ITokenService tokenProvider)
    {
        _tokenProvider = tokenProvider;

        _policy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            .RetryAsync((_, _) => tokenProvider.RefreshToken());
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => await _policy.ExecuteAsync(async () =>
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokenProvider.GetToken());
            return await base.SendAsync(request, cancellationToken);
        });
}
