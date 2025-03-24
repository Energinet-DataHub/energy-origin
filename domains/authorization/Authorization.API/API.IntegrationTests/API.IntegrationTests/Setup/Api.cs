using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Authorization.Controllers;

namespace API.IntegrationTests.Setup;

public class Api : IAsyncLifetime
{
    private readonly HttpClient _client;
    public readonly JsonSerializerOptions SerializerOptions;

    private JsonSerializerOptions JsonSerializerOptions()
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

    public async Task<HttpResponseMessage> GrantConsentToClient(Guid clientId)
    {
        var request = new GrantConsentToClientRequest(clientId);
        return await _client.PostAsJsonAsync("/api/authorization/consent/client/grant", request);
    }

    public async Task<HttpResponseMessage> GetServiceProviderTerms()
    {
        return await _client.GetAsync("/api/authorization/service-provider-terms");
    }

    public async Task<HttpResponseMessage> GrantConsentToOrganization(Guid organizationId)
    {
        var request = new GrantConsentToOrganizationRequest(organizationId);
        return await _client.PostAsJsonAsync("/api/authorization/consent/organization/grant", request);
    }

    public async Task<HttpResponseMessage> GetClient(Guid idpClientId)
    {
        return await _client.GetAsync("/api/authorization/client/" + idpClientId);
    }

    public async Task<HttpResponseMessage> GetOrganization(Guid organizationId)
    {
        return await _client.GetAsync("/api/authorization/organization/" + organizationId);
    }

    public async Task<HttpResponseMessage> GetFirstPartyOrganizations()
    {
        return await _client.GetAsync("/api/authorization/admin-portal/first-party-organizations/");
    }

    public async Task<HttpResponseMessage> GetWhitelistedOrganizations()
    {
        return await _client.GetAsync("/api/authorization/admin-portal/whitelisted-organizations/");
    }

    public async Task<HttpResponseMessage> AcceptTerms()
    {
        return await _client.PostAsJsonAsync("/api/authorization/terms/accept", new { }, SerializerOptions);
    }

    public async Task<HttpResponseMessage> RevokeTerms()
    {
        return await _client.PostAsJsonAsync("/api/authorization/terms/revoke", new { }, SerializerOptions);
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

    public async Task<HttpResponseMessage> GetUserOrganizationReceivedConsents()
    {
        return await _client.GetAsync("/api/authorization/consents/organization/received");
    }

    public async Task<HttpResponseMessage> DeleteConsent(Guid consentId)
    {
        return await _client.DeleteAsync($"/api/authorization/consents/{consentId}");
    }

    public ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
