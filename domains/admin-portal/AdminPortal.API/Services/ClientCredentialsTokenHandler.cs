using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace AdminPortal.API.Services;

public class ClientCredentialsTokenHandler : DelegatingHandler
{
    private readonly IConfidentialClientApplication _confidentialClient;
    private readonly string[] _scopes;

    public ClientCredentialsTokenHandler(string clientId, string clientSecret, string tenantId, string[] scopes)
    {
        _scopes = scopes;
        _confidentialClient = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
            .Build();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var result = await _confidentialClient.AcquireTokenForClient(_scopes).ExecuteAsync(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
