using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;

namespace AdminPortal.Services;

public interface IAuthorizationService
{
    Task<GetFirstPartyOrganizationsResponse> GetOrganizationsHttpRequestAsync();
    Task<GetWhitelistedOrganizationsResponse> GetWhitelistedOrganizationsHttpRequestAsync();
    Task AddOrganizationToWhitelistHttpRequestAsync(string tin);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly HttpClient _client;

    public AuthorizationService(HttpClient client)
    {
        _client = client;
    }

    public async Task<GetFirstPartyOrganizationsResponse> GetOrganizationsHttpRequestAsync()
    {
        var response = await _client.GetAsync("first-party-organizations/");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetFirstPartyOrganizationsResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }

    public async Task<GetWhitelistedOrganizationsResponse> GetWhitelistedOrganizationsHttpRequestAsync()
    {
        var response = await _client.GetAsync("whitelisted-organizations/");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetWhitelistedOrganizationsResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }

    public async Task AddOrganizationToWhitelistHttpRequestAsync(string tin)
    {
        var response = await _client.PostAsJsonAsync("whitelisted-organizations/", new { Tin = tin });
        response.EnsureSuccessStatusCode();
    }
}
