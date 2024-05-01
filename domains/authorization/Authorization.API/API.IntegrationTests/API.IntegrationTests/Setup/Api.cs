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
