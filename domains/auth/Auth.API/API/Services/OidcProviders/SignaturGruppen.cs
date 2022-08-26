using API.Configuration;
using API.Controllers.dto;
using API.Models;
using API.Errors;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text;

namespace API.Services;

public class SignaturGruppen : IOidcProviders
{
    readonly IOidcService _oidcService;
    readonly AuthOptions _authOptions;
    readonly ILogger<SignaturGruppen> _logger;
    readonly HttpClient _httpClient;

    public SignaturGruppen(ILogger<SignaturGruppen> logger, IOidcService oidcService, IOptions<AuthOptions> authOptions, HttpClient httpClient)
    {
        _logger = logger;
        _oidcService = oidcService;
        _authOptions = authOptions.Value;
        _httpClient = httpClient;
    }

    public NextStep CreateAuthorizationUri(AuthState state)
    {
        var query = _oidcService.CreateAuthorizationRedirectUrl("code", state, "en");

        var amrValues = new Dictionary<string, string>()
        {
            { "amr_values", _authOptions.AmrValues }
        };
        var nemId = new Dictionary<string, Dictionary<string, string>>()
        {
            { "nemid", amrValues }
        };

        query.Add("idp_params", JsonSerializer.Serialize(nemId));
        
        var authorizationUri = new NextStep() { NextUrl = _authOptions.AuthorityUrl + query.ToString() };

        return authorizationUri;
    }

    // T makes sure we can pass a dynamic object type I.E NemID and MitID
    public async Task<T> FetchUserInfo<T>(OidcTokenResponse oidcToken)
    {
        string uri = $"{_authOptions.AuthorityUrl}/connect/userinfo";

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"{oidcToken.TokenType} {oidcToken.AccessToken}");

        var res = await _httpClient.GetAsync(uri);

        if (res.StatusCode != HttpStatusCode.OK)
        {
            // This should be changes to have better logging
            _logger.LogCritical(res.StatusCode.ToString());
            throw new HttpRequestException(res.StatusCode.ToString());
        }

        var data = JsonSerializer.Deserialize<T>(res.Content.ToString()!);

        return data!;
    }
    public async Task<JsonElement> FetchToken(AuthState state, string code, string redirectUri)
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
        return token;
    }

    public bool isError(OidcCallbackParams oidcCallbackParams)
    {
        return oidcCallbackParams.Error != null || oidcCallbackParams.ErrorDescription != null;
    }

    public NextStep OnOidcFlowFailed(AuthState authState, OidcCallbackParams oidcCallbackParams)
    {
        AuthError error = AuthError.UnknownErrorFromIdentityProvider;

        if (oidcCallbackParams.ErrorDescription != null)
        {
            if (oidcCallbackParams.ErrorDescription == "mitid_user_aborted" || oidcCallbackParams.ErrorDescription == "user_aborted")
            {
                error = AuthError.UserInterrupted;
            }
        }

        return BuildFailureUrl(authState, error);
    }

    public NextStep BuildFailureUrl(AuthState authState, AuthError error)
    {
        var query = new QueryBuilder
        {
            { "success", "0" },
            { "error_code", error.ErrorCode },
            { "error", error.ErrorDescription },
        };

        var errorUrl = new NextStep() { NextUrl = authState.ReturnUrl + query.ToString() };

        return errorUrl;
    }
}
