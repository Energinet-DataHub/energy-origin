using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using EnergyOrigin.Domain.ValueObjects;

namespace AdminPortal.Services;

public interface IAuthorizationService
{
    Task<GetOrganizationsResponse> GetOrganizationsAsync(CancellationToken cancellationToken);
    Task<GetWhitelistedOrganizationsResponse> GetWhitelistedOrganizationsAsync(CancellationToken cancellationToken);
    Task AddOrganizationToWhitelistAsync(Tin tin, CancellationToken cancellationToken);
    Task RemoveOrganizationFromWhitelistAsync(Tin tin, CancellationToken cancellationToken);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly HttpClient _client;

    public AuthorizationService(HttpClient client)
    {
        _client = client;
    }

    public async Task<GetOrganizationsResponse> GetOrganizationsAsync(CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync("first-party-organizations/", cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetOrganizationsResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }

    public async Task<GetWhitelistedOrganizationsResponse> GetWhitelistedOrganizationsAsync(CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync("whitelisted-organizations/", cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetWhitelistedOrganizationsResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }

    public async Task AddOrganizationToWhitelistAsync(Tin tin, CancellationToken cancellationToken)
    {
        var response = await _client.PostAsJsonAsync("whitelisted-organizations/", new { Tin = tin.Value }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveOrganizationFromWhitelistAsync(Tin tin, CancellationToken cancellationToken)
    {
        var response = await _client.DeleteAsync($"whitelisted-organizations/{tin}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
