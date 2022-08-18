using API.Helpers;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text.Json;
using System.Net;
using API.Models.Oidc;

namespace API.Services;
public class OidcService : IOidcService
{
    readonly ILogger _logger;
    readonly ICryptographyService _cryptography;
    readonly HttpClient _httpClient;
    public OidcService(ILogger<OidcService> logger, ICryptographyService cryptography, HttpClient httpClient)
    {
        _logger = logger;
        _cryptography = cryptography;
        _httpClient = httpClient;
    }

    public QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var serilizedJson = JsonSerializer.Serialize(state);


        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", Configuration.GetOidcClientId() },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", Configuration.GetScopes() },
            { "state", _cryptography.Encrypt(serilizedJson) },
            { "language", lang }
        };

        return query;
    }

    public async Task<OidcTokenResponse> FetchToken(AuthState state, string code)
    {
        string uri = $"{Configuration.GetAuthorityUrl}/connect/token";

        OidcToken jsonData = new OidcToken()
        {
            GrantType = "authorization_code",
            RedirectUrl = state.ReturnUrl,
            Code = code,
            ClientId = Configuration.GetOidcClientId(),
            ClientSecret = Configuration.GetOidcClientSecret()
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
