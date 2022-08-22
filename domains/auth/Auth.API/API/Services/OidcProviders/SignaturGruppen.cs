using API.Configuration;
using API.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Net;

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

        if (state.CustomerType == "company")
        {
            var amrValues = new Dictionary<string, string>()
        {
            { "amr_values", _authOptions.AmrValues }
        };
            var nemId = new Dictionary<string, Dictionary<string, string>>()
        {
            { "nemid", amrValues }
        };

            query.Add("idp_params", JsonSerializer.Serialize(nemId));
            query.Add("private_to_business", "true");
        }
        else if (state.CustomerType == "private")
        {
            query.Add("scope", "mitid");
        }

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
}
