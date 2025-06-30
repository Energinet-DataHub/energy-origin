using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.Datahub3;

public interface ITokenService
{
    Task<string> GetToken();
}

public class TokenService : ITokenService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<DataHub3Options> _dataHub3Options;
    private readonly TimeProvider _timeProvider;

    private readonly FormUrlEncodedContent _body;
    private Token? _token;
    private readonly SemaphoreSlim _semaphore;

    private readonly int _tokenLifetimeSeconds = 3600;
    private readonly int _halvingFactor = 2;

    public TokenService(HttpClient httpclient, IOptions<DataHub3Options> dataHub3Options, TimeProvider timeProvider)
    {
        _httpClient = httpclient;
        _dataHub3Options = dataHub3Options;
        _timeProvider = timeProvider;

        var bodyValues = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = dataHub3Options.Value.ClientId!,
            ["client_secret"] = dataHub3Options.Value.ClientSecret!,
            ["scope"] = dataHub3Options.Value.Scope!
        };
        _body = new FormUrlEncodedContent(bodyValues);

        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<string> GetToken()
    {
        if (_dataHub3Options.Value.EnableMock)
        {
            return Guid.NewGuid().ToString();
        }

        if (_token is not null && !IsTokenStaleOrExpired())
        {
            return _token.AccessToken;
        }

        await _semaphore.WaitAsync();
        try
        {
            // Double-checked locking
            if (_token is not null && !IsTokenStaleOrExpired())
            {
                return _token.AccessToken;
            }

            return await RefreshToken();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> RefreshToken()
    {
        var response = await _httpClient.PostAsync(_dataHub3Options.Value.TokenUrl, _body);

        if (response.IsSuccessStatusCode)
        {
            _token = JsonSerializer.Deserialize<Token>(await response.Content.ReadAsStringAsync());
            return _token!.AccessToken;
        }

        return string.Empty;
    }

    private bool IsTokenStaleOrExpired()
    {
        if (_token is null)
        {
            return true;
        }

        var difference = _token.Expires - _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        if (difference < _tokenLifetimeSeconds / _halvingFactor)
        {
            return true;
        }

        return false;
    }
}
