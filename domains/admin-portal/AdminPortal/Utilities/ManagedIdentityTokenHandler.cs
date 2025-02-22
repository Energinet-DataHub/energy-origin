using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace AdminPortal.Utilities;

public class ManagedIdentityTokenHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;

    public ManagedIdentityTokenHandler()
    {
        _credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions { ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") }
        );
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var tokenRequestContext = new TokenRequestContext(new[] { "api://ett-internal/.default" });
        var accessToken = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}
