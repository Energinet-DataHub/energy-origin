using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ProjectOriginClients;


public interface IClientCredentialsService
{
    Task<string> GetAccessTokenAsync();
}

public class ClientCredentialsOptions
{
    public const string Prefix = "ClientCredentials";

    [Required]
    public string ClientID { get; set; } = null!;
    [Required]
    public string ClientSecret { get; set; } = null!;
    [Required]
    public string Scope { get; set; } = null!;
    [Required]
    public string TokenUrl { get; set; } = null!;
}


public class ClientCredentialsService(IOptions<ClientCredentialsOptions> clientCredentialsOptions, HttpClient httpClient) : IClientCredentialsService
{
    private string _accessToken = string.Empty;
    private DateTime _accessTokenExpiryTime;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly ClientCredentialsOptions _clientCredentialsOptions = clientCredentialsOptions.Value;
    private readonly int _renewTokenBeforeExpirySeconds = 60;

    public async Task<string> GetAccessTokenAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_accessTokenExpiryTime <= DateTime.UtcNow)
            {
                await RenewTokenAsync();
            }
            return _accessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RenewTokenAsync()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _clientCredentialsOptions.ClientID),
            new KeyValuePair<string, string>("client_secret", _clientCredentialsOptions.ClientSecret),
            new KeyValuePair<string, string>("scope", _clientCredentialsOptions.Scope)
        });

        HttpResponseMessage response = await httpClient.PostAsync("", content);

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadFromJsonAsync<TokenResponse>();

        _accessToken = responseBody!.AccessToken;
        _accessTokenExpiryTime = DateTime.UtcNow.AddSeconds(responseBody.ExpiresIn - _renewTokenBeforeExpirySeconds);
    }


}

public class TokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = null!;

    [JsonPropertyName("token_type")] public string TokenType { get; set; } = null!;

    [JsonPropertyName("not_before")] public string NotBefore { get; set; } = null!;

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

    [JsonPropertyName("expires_on")] public string ExpiresOn { get; set; } = null!;

    [JsonPropertyName("resource")] public string Resource { get; set; } = null!;
}
