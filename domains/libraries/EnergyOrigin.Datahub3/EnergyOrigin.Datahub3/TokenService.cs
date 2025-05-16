using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.Datahub3;

public interface ITokenService
{
    Task<string> GetToken();
    Task<string> RefreshToken();
}

public class TokenService : ITokenService
{
    readonly HttpClient _httpClient;
    readonly IOptions<DataHub3Options> _dataHub3Options;
    readonly FormUrlEncodedContent _body;
    Token? _token;

    public TokenService(HttpClient httpclient, IOptions<DataHub3Options> dataHub3Options)
    {
        _httpClient = httpclient;
        _dataHub3Options = dataHub3Options;
        var bodyValues = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = dataHub3Options.Value.ClientId!,
            ["client_secret"] = dataHub3Options.Value.ClientSecret!,
            ["scope"] = dataHub3Options.Value.Scope!
        };
        _body = new FormUrlEncodedContent(bodyValues);
    }

    public async Task<string> GetToken()
    {
        if (_dataHub3Options.Value.EnableMock)
        {
            return Guid.NewGuid().ToString();
        }

        if (_token == null)
            return await RefreshToken();

        return _token.AccessToken;
    }

    public async Task<string> RefreshToken()
    {
        if (_dataHub3Options.Value.EnableMock)
        {
            return Guid.NewGuid().ToString();
        }

        var response = await _httpClient.PostAsync(_dataHub3Options.Value.TokenUrl, _body);

        if (response.IsSuccessStatusCode)
        {
            _token = JsonSerializer.Deserialize<Token>(await response.Content.ReadAsStringAsync());
            return _token!.AccessToken;
        }

        return string.Empty;
    }
}
