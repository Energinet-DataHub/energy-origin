using System.Net;
using System.Text;
using System.Text.Json;
using API.Configuration;
using API.Controllers.dto;
using API.Errors;
using API.Models;
using API.Utilities;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace API.Services.OidcProviders;

public class SignaturGruppen : IOidcService
{
    private readonly AuthOptions authOptions;
    private readonly ILogger<SignaturGruppen> logger;
    private readonly HttpClient httpClient;
    private readonly ICryptography stateCryptography;
    //private readonly IJwkService jwkService;

    public SignaturGruppen(
        ILogger<SignaturGruppen> logger,
        IOptions<AuthOptions> authOptions,
        HttpClient httpClient,
        ICryptography cryptography,
        IJwkService jwkService
        )
    {
        this.logger = logger;
        this.authOptions = authOptions.Value;
        this.httpClient = httpClient;
        //this.jwkService = jwkService;
        stateCryptography = cryptography;
    }

    public NextStep CreateAuthorizationUri(AuthState state)
    {
        var amrValues = new Dictionary<string, string>()
        {
            { "amr_values", authOptions.AmrValues }
        };
        var nemId = new Dictionary<string, Dictionary<string, string>>()
        {
            { "nemid", amrValues }
        };

        var query = CreateAuthorizationRedirectUrl("code", state, "en");

        query.Add("idp_params", JsonSerializer.Serialize(nemId));

        var authorizationUri = new NextStep() { NextUrl = authOptions.OidcUrl + query };

        return authorizationUri;
    }

    public async Task<OidcTokenResponse> FetchToken(string code)
    {
        var redirectUri = authOptions.ServiceUrl + authOptions.OidcLoginCallbackPath;

        var url = $"{authOptions.OidcUrl}/connect/token";

        var valueBytes = Encoding.UTF8.GetBytes($"{authOptions.OidcClientId}:{authOptions.OidcClientSecret}");
        var authorization = Convert.ToBase64String(valueBytes);

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, url);
        tokenRequest.Headers.Add("Authorization", $"Basic {authorization}");
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", redirectUri }
        });

        var tokenResponse = await httpClient.SendAsync(tokenRequest);

        if (tokenResponse.StatusCode != HttpStatusCode.OK)
        {
            logger.LogDebug($"FetchToken: tokenResponse: {tokenResponse.StatusCode}");
            logger.LogDebug($"connect/token: authorization header: {tokenRequest.Headers}");
            throw new HttpRequestException(tokenResponse.StatusCode.ToString());
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

        var encoded = JsonSerializer.Deserialize<OidcTokenResponse>(tokenJson);

        return encoded != null ? encoded : throw new FormatException();

        //return encoded != null ? DecodeOidcResponse(encoded) : throw new FormatException();  // FIXME Missing Verification of tokens with "/.well-known/openid-configuration/jwks";
    }

    public NextStep OnOidcFlowFailed(AuthState authState, OidcCallbackParams oidcCallbackParams)
    {
        var error = AuthError.UnknownErrorFromIdentityProvider;

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

    public async Task Logout(string token)
    {
        var url = authOptions.OidcUrl;
        httpClient.BaseAddress = new Uri(url);

        var response = await httpClient.PostAsJsonAsync("/api/v1/session/logout", new { id_token = token });
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            logger.LogWarning("StatusCode: {StatusCode}, url: {Url}, content: {Content}",
                response.StatusCode, response.RequestMessage?.RequestUri, content);
        }
    }

    private QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var json = JsonSerializer.Serialize(state);

        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", authOptions.OidcClientId },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", "openid,mitid,nemid,userinfo_token" },
            { "state", stateCryptography.Encrypt(json) },
            { "language", lang }
        };

        return query;
    }
}
