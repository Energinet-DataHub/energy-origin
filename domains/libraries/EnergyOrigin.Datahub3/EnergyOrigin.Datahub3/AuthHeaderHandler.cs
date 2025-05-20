using System.Net;
using System.Net.Http.Headers;
using Polly;
using Polly.Retry;

namespace EnergyOrigin.Datahub3;

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
