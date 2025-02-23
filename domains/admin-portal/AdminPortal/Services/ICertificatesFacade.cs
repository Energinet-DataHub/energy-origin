using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Dtos;

namespace AdminPortal.Services;

public interface ICertificatesFacade
{
    Task<ContractsForAdminPortalResponse> GetContractsAsync();
}

public class CertificatesFacade : ICertificatesFacade
{
    private readonly HttpClient _client;

    public CertificatesFacade(HttpClient client)
    {
        _client = client;
    }

    public async Task<ContractsForAdminPortalResponse> GetContractsAsync()
    {
        var response = await _client.GetAsync("internal-contracts-workload/");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ContractsForAdminPortalResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }
}
