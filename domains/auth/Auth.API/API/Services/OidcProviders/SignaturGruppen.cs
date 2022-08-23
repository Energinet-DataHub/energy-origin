using System.Text.Json;
using API.Configuration;
using API.Helpers;
using API.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Extensions;

namespace API.Services.OidcProviders;

public class SignaturGruppen : IOidcService
{
    private readonly AuthOptions _authOptions;
    private readonly ILogger<SignaturGruppen> _logger;
    private readonly HttpClient _httpClient;
    private readonly ICryptography _cryptography;

    public SignaturGruppen(ILogger<SignaturGruppen> logger, IOptions<AuthOptions> authOptions,
        HttpClient httpClient, ICryptography cryptography)
    {
        _logger = logger;
        _authOptions = authOptions.Value;
        _httpClient = httpClient;
        _cryptography = cryptography;
    }

    public NextStep CreateAuthorizationUri(AuthState state)
    {
        var amrValues = new Dictionary<string, string>()
        {
            { "amr_values", _authOptions.AmrValues }
        };
        var nemId = new Dictionary<string, Dictionary<string, string>>()
        {
            { "nemid", amrValues }
        };

        var query = CreateAuthorizationRedirectUrl("code", state, "en");

        query.Add("idp_params", JsonSerializer.Serialize(nemId));

        var authorizationUri = new NextStep() { NextUrl = _authOptions.OidcUrl + query };

        return authorizationUri;
    }

    public async Task Logout(string token)
    {
        var url = _authOptions.OidcUrl;
        _httpClient.BaseAddress = new Uri(url);

        var response = await _httpClient.PostAsJsonAsync("/api/v1/session/logout", new { id_token = token });
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogWarning("StatusCode: {StatusCode}, url: {Url}, content: {Content}",
                response.StatusCode, response.RequestMessage?.RequestUri, content);
        }
    }

    private QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var serilizedJson = JsonSerializer.Serialize(state);

        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", _authOptions.OidcClientId },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", _authOptions.Scope },
            { "state", _cryptography.Encrypt(serilizedJson) },
            { "language", lang }
        };

        return query;
    }
}
