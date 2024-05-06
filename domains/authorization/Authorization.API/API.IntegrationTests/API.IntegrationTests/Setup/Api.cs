using System.Net.Http.Json;
using API.Authorization._Features_;
using API.Authorization.Controllers;

namespace API.IntegrationTests.Setup;

public class Api : IAsyncLifetime
{
    private readonly HttpClient _client;

    public Api(HttpClient client)
    {
        _client = client;
    }

    public async Task<HttpResponseMessage> GrantConsent(Guid clientId)
    {
        var request = new GrantConsentRequest(clientId);
        return await _client.PostAsJsonAsync("/api/consent/grant", request);
    }

    public async Task<HttpResponseMessage> GetConsent(Guid clientId)
    {
        return await _client.GetAsync("/api/consent/grant/" + clientId);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }
}
