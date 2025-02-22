using System;
using Azure.Core;
using Azure.Identity;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AdminPortal.Utilities;

public class ManagedIdentityTokenHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;
    private readonly string _audience;

    public ManagedIdentityTokenHandler()
    {
        _credential = new DefaultAzureCredential();
        _audience = Environment.GetEnvironmentVariable("AKS_MANAGED_IDENTITY_CLIENT_ID")
                    ?? throw new InvalidOperationException("AKS_MANAGED_IDENTITY_CLIENT_ID is not set");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenRequest = new TokenRequestContext(new[] { $"{_audience}/.default" });
        var accessToken = await _credential.GetTokenAsync(tokenRequest, cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

        return await base.SendAsync(request, cancellationToken);
    }
}
