using System.Text.Json;
using Proxy.Controllers;

namespace Proxy.Services;

public class WalletProxyService : IWalletProxyService
{
    private readonly HttpClient _client;


    public WalletProxyService(HttpClient client)
    {
        _client = client;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string path, string orgId)
    {
        _client.DefaultRequestHeaders.Add(WalletConstants.Header, orgId);

        var response = await _client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> PostAsync<TResponse, TRequest>(string apiUrl, TRequest data, string orgId)
    {
        _client.DefaultRequestHeaders.Add(WalletConstants.Header, orgId);

        var jsonData =  JsonSerializer.Serialize(data);
        var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(apiUrl, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public void GetProxyInformation()
    {

    }
}
