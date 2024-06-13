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
        return await _client.PostAsJsonAsync("/api/authorization/consent/grant", request);
    }

    public async Task<HttpResponseMessage> GetConsent(Guid clientId)
    {
        return await _client.GetAsync("/api/authorization/consent/grant/" + clientId);
    }

    public async Task<HttpResponseMessage> GetClient(Guid idpClientId)
    {
        return await _client.GetAsync("/api/authorization/client/" + idpClientId);
    }

    public async Task<HttpResponseMessage> GetClientConsents()
    {
        return await _client.GetAsync("/api/authorization/client/consents/");
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
