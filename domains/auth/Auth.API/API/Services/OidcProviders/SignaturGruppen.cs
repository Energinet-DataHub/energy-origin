using API.Helpers;
using API.Models;
using System.Text.Json;

namespace API.Services;

public class SignaturGruppen : IOidcProviders
{
    readonly IOidcService oidcService;
    readonly ILogger<SignaturGruppen> logger;

    public SignaturGruppen(ILogger<SignaturGruppen> logger, IOidcService oidcService)
    {
        this.logger = logger;
        this.oidcService = oidcService;
    }

    public NextStep CreateAuthorizationUri(AuthState state)
    {
        var amrValues = new Dictionary<string, string>()
        {
            { "amr_values", Configuration.GetAmrValues() }
        };
        var nemId = new Dictionary<string, Dictionary<string, string>>()
        {
            { "nemid", amrValues}
        };

        var query = oidcService.CreateAuthorizationRedirectUrl("code", state, "en");

        query.Add("idp_params", JsonSerializer.Serialize(nemId));

        var authorizationUri = new NextStep() { NextUrl = Configuration.GetAuthorityUrl() + query.ToString() };

        return authorizationUri;
    }
}
