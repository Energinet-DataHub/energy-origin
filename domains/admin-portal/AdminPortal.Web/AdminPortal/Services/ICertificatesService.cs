using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;

namespace AdminPortal.Services;

public interface ICertificatesService
{
    Task<GetContractsForAdminPortalResponse> GetContractsHttpRequestAsync();
}

public class CertificatesService : ICertificatesService
{
    private readonly HttpClient _client;

    public CertificatesService(HttpClient client)
    {
        _client = client;
    }

    public async Task<GetContractsForAdminPortalResponse> GetContractsHttpRequestAsync()
    {
        var response = await _client.GetAsync("internal-contracts/");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetContractsForAdminPortalResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }
}
