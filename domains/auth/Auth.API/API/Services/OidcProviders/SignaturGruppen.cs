using System.Text.Json;
using API.Configuration;
using API.Helpers;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace API.Services.OidcProviders;

public class SignaturGruppen : IOidcService
{
    private readonly IOidcService oidcService;
    private readonly AuthOptions authOptions;
    private readonly ILogger<SignaturGruppen> logger;
    private readonly HttpClient httpClient;
    private readonly ICryptography cryptography;

    public SignaturGruppen(
        ILogger<SignaturGruppen> logger,
        IOptions<AuthOptions> authOptions,
        HttpClient httpClient, ICryptography cryptography
    )
    {
        this.logger = logger;
        this.authOptions = authOptions.Value;
        this.httpClient = httpClient;
        this.cryptography = cryptography;
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
        var serilizedJson = JsonSerializer.Serialize(state);

        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", authOptions.OidcClientId },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", authOptions.Scope },
            { "state", cryptography.Encrypt(serilizedJson) },
            { "language", lang }
        };

        return query;
    }
}
