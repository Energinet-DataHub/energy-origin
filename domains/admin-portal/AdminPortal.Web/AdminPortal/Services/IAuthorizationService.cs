using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using EnergyOrigin.Domain.ValueObjects;

namespace AdminPortal.Services;

public interface IAuthorizationService
{
    Task<GetOrganizationsResponse> GetOrganizationsHttpRequestAsync();
    Task<GetWhitelistedOrganizationsResponse> GetWhitelistedOrganizationsHttpRequestAsync();
    Task AddOrganizationToWhitelistHttpRequestAsync(Tin tin);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly HttpClient _client;

    public AuthorizationService(HttpClient client)
    {
        _client = client;
    }

    public async Task<GetOrganizationsResponse> GetOrganizationsHttpRequestAsync()
    {
        var response = await _client.GetAsync("first-party-organizations/");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetOrganizationsResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }

    public async Task<GetWhitelistedOrganizationsResponse> GetWhitelistedOrganizationsHttpRequestAsync()
    {
        var response = await _client.GetAsync("whitelisted-organizations/");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetWhitelistedOrganizationsResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }

    public async Task AddOrganizationToWhitelistHttpRequestAsync(Tin tin)
    {
        var response = await _client.PostAsJsonAsync("whitelisted-organizations/", new { Tin = tin.Value });
        response.EnsureSuccessStatusCode();
    }
}
