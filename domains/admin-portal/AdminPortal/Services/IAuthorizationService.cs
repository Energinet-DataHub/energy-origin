using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Dtos;

namespace AdminPortal.Services;

public interface IAuthorizationService
{
    Task<FirstPartyOrganizationsResponse> GetOrganizationsAsync();
}

public class AuthorizationService : IAuthorizationService
{
    private readonly HttpClient _client;

    public AuthorizationService(HttpClient client)
    {
        _client = client;
    }

    public async Task<FirstPartyOrganizationsResponse> GetOrganizationsAsync()
    {
        var response = await _client.GetAsync("first-party-organizations/");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FirstPartyOrganizationsResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }
}
