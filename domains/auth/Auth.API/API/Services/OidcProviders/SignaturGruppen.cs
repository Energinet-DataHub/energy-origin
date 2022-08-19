using System.Text.Json;
using API.Configuration;
using API.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace API.Services.OidcProviders;

public class SignaturGruppen : IOidcProviders
{
    readonly IOidcService oidcService;
    private readonly AuthOptions authOptions;
    readonly ILogger<SignaturGruppen> logger;
    private readonly HttpClient httpClient;

    public SignaturGruppen(ILogger<SignaturGruppen> logger, IOidcService oidcService, IOptions<AuthOptions> authOptions,
        HttpClient httpClient)
    {
        this.logger = logger;
        this.oidcService = oidcService;
        this.authOptions = authOptions.Value;
        this.httpClient = httpClient;
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

        var query = oidcService.CreateAuthorizationRedirectUrl("code", state, "en");

        query.Add("idp_params", JsonSerializer.Serialize(nemId));

        var authorizationUri = new NextStep() { NextUrl = authOptions.OidcUrl + query.ToString() };

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
}
