using API.Configuration;
using API.Helpers;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;
using Jose;

namespace API.Services;

public class OidcService : IOidcService
{
    readonly ILogger _logger;
    readonly ICryptographyService _cryptography;
    private readonly AuthOptions _authOptions;

    readonly HttpClient _httpClient;
    public OidcService(ILogger<OidcService> logger, ICryptographyService cryptography, IOptions<AuthOptions> authOptions, HttpClient httpClient)
    {
        _logger = logger;
        _cryptography = cryptography;
        _httpClient = httpClient;
        _authOptions = authOptions.Value;
    }

    public QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var serilizedJson = JsonSerializer.Serialize(state);

        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", _authOptions.OidcClientId },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", _authOptions.Scope},
            { "state", _cryptography.Encrypt(serilizedJson) },
            { "language", lang }
        };

        return query;
    }

    public async Task<OidcTokenResponse> FetchToken(AuthState state, string code, string redirectUri)
    {
        string url = $"{_authOptions.AuthorityUrl}/connect/token";

        var valueBytes = Encoding.UTF8.GetBytes($"{_authOptions.OidcClientId}:{_authOptions.OidcClientSecret}");
        var authorization = Convert.ToBase64String(valueBytes);

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, url);
        tokenRequest.Headers.Add("Authorization", $"Basic {authorization}");
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", redirectUri }
        });
        var tokenResponse = await _httpClient.SendAsync(tokenRequest);

        if (tokenResponse.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogDebug($"FetchToken: tokenResponse: {tokenResponse.StatusCode}");
            _logger.LogDebug($"connect/token: authorization header: {tokenRequest.Headers}");
            throw new HttpRequestException(tokenResponse.StatusCode.ToString());
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(tokenJson).RootElement;

        var idTokenJwt = token.GetProperty("id_token").GetString()!;
        var idTokenJson = idTokenJwt.GetJwtPayload();
        var idToken = JsonDocument.Parse(idTokenJson).RootElement;





        var data = JsonSerializer.Deserialize<OidcTokenResponse>(tokenResponse.Content.ToString()!);

        return data!;
    }

    public string EncodeBase64(this string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(valueBytes);
    }

    public async Task<string> GetJwkAsync()
    {
        var jwkResponse = await _httpClient.GetAsync($"{_authOptions.AuthorityUrl}/.well-known/openid-configuration/jwks");
        var jwkSet = JwkSet.FromJson(await jwkResponse.Content.ReadAsStringAsync(), new JsonMapper());
        var jwk = jwkSet.Keys.Single();


        if (jwksResponse.StatusCode != HttpStatusCode.OK)
        {
            // This should be changes to have better logging
            _logger.LogCritical(jwksResponse.StatusCode.ToString());
            throw new HttpRequestException(jwksResponse.StatusCode.ToString());
        }

        var jwks = await jwksResponse.Content.ReadAsStringAsync();

        return jwks;
    }

}
