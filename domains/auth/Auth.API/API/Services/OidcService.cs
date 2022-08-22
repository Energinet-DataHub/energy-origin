using API.Configuration;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Net;

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
            { "scope", "openid" },
            { "state", _cryptography.Encrypt(serilizedJson) },
            { "language", lang }
        };

        return query;
    }

    public async Task<OidcTokenResponse> FetchToken(AuthState state, string code)
    {
        string uri = $"{_authOptions.AuthorityUrl}/connect/token";

        OidcToken jsonData = new OidcToken()
        {
            GrantType = "authorization_code",
            RedirectUrl = state.ReturnUrl,
            Code = code,
            ClientId = _authOptions.OidcClientId,
            ClientSecret = _authOptions.OidcClientSecret
        };

        var res = await _httpClient.PostAsJsonAsync(uri, jsonData);

        if (res.StatusCode != HttpStatusCode.OK)
        {
            // This should be changes to have better logging
            _logger.LogCritical(res.StatusCode.ToString());
            throw new HttpRequestException(res.StatusCode.ToString());
        }

        var data = JsonSerializer.Deserialize<OidcTokenResponse>(res.Content.ToString()!);

        return data!;
    }
}
