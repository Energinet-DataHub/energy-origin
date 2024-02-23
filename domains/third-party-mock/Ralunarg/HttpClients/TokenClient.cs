using Ralunarg.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Ralunarg.Options;

namespace Ralunarg.HttpClients;
public class TokenClient
{
    private readonly AuthTenantOptions _tenantOptions;

    private readonly HttpClient _httpClient;

    public TokenClient(HttpClient httpClient, IOptions<AuthTenantOptions> tenantOptions)
    {
        _tenantOptions = tenantOptions.Value;
        _httpClient = httpClient;
    }

    public async Task<JwtToken> GetToken()
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new ("client_id", _tenantOptions.ClientId),
            new ("client_secret", _tenantOptions.ClientSecret),
            new ("grant_type", "client_credentials"),
            new ("scope", _tenantOptions.Scope),
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "f7619355-6c67-4100-9a78-1847f30742e2/oauth2/v2.0/token")
        {
            Content = new FormUrlEncodedContent(parameters)
        };

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var token = await response.Content.ReadFromJsonAsync<JwtToken>();

            if (token is null)
                throw new Exception("Something went wrong when getting the token!");

            return token;
        }

        throw new Exception("Something went wrong when getting the token!");
    }

}
