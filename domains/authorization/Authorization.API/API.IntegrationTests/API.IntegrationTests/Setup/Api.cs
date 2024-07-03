using System.Net.Http.Formatting;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Authorization.Controllers;

namespace API.IntegrationTests.Setup;

public class Api : IAsyncLifetime
{
    private readonly HttpClient _client;
    public readonly JsonSerializerOptions SerializerOptions;

    internal JsonSerializerOptions JsonSerializerOptions()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        return jsonOptions;
    }

    public Api(HttpClient client)
    {
        SerializerOptions = JsonSerializerOptions();
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

    public async Task<HttpResponseMessage> GetConsentForUser(AuthorizationUserRequest request)
    {
        return await _client.PostAsJsonAsync("/api/authorization/user-consent/", request, SerializerOptions);
    }

    public async Task<HttpResponseMessage> CreateClient(Guid idpClientId, string name, ClientType clientType,
        string redirectUrl)
    {
        var request = new CreateClientRequest(idpClientId, name, clientType, redirectUrl);
        return await _client.PostAsJsonAsync("/api/authorization/Admin/Client", request, SerializerOptions);
    }

    public async Task<HttpResponseMessage> GetUserOrganizationConsents()
    {
        return await _client.GetAsync("/api/authorization/consents/");
    }

    public async Task<HttpResponseMessage> DeleteConsent(Guid clientId)
    {
        return await _client.DeleteAsync($"/api/authorization/consents/{clientId}");
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
