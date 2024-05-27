using System.Net.Http.Json;

namespace AccessControl.IntegrationTests.Setup;

public class Api : IAsyncLifetime
{
    private readonly HttpClient _client;

    public Api(HttpClient client)
    {
        _client = client;
    }

    public async Task<HttpResponseMessage> Decision(Guid organizationId)
    {
        return await _client.GetAsync($"api/authorization/access-control?organizationId={organizationId}/");
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
