using System.Text.Json;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<TokenService> _logger;
    private readonly FormUrlEncodedContent _body;

    private readonly SemaphoreSlim _semaphore;

    private Token? _token;
    private long _expiresOn;

    private readonly int _halvingFactor = 2;

    public TokenService(HttpClient httpclient, IOptions<DataHub3Options> dataHub3Options, TimeProvider timeProvider, ILogger<TokenService> logger)
    {
        _httpClient = httpclient;
        _dataHub3Options = dataHub3Options;
        _timeProvider = timeProvider;
        _logger = logger;

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

        _logger.LogInformation("Token is stale or expired. Refreshing");

        if (response.IsSuccessStatusCode)
        {
            _token = JsonSerializer.Deserialize<Token>(await response.Content.ReadAsStringAsync());
            if (_token is not null)
            {
                _expiresOn = _timeProvider.GetUtcNow().ToUnixTimeSeconds() + _token.ExpiresIn;
                _logger.LogInformation("New token fetched. Expires on: {ExpiresOn}", _expiresOn);
            }

            return _token!.AccessToken;
        }

        _logger.LogWarning("New token could not be fetched.");
        return string.Empty;
    }

    private bool IsTokenStaleOrExpired()
    {
        if (_token is null)
        {
            return true;
        }

        var difference = _expiresOn - _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        if (difference < _token.ExpiresIn / _halvingFactor)
        {
            return true;
        }

        return false;
    }
}
