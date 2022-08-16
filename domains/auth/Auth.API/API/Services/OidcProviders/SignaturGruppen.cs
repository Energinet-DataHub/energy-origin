using API.Configuration;
using API.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace API.Services;

public class SignaturGruppen : IOidcProviders
{
    private readonly IOidcService oidcService;
    private readonly AuthOptions authOptions;
    private readonly ILogger<SignaturGruppen> logger;

    public SignaturGruppen(ILogger<SignaturGruppen> logger, IOidcService oidcService, IOptions<AuthOptions> authOptions)
    {
        this.logger = logger;
        this.oidcService = oidcService;
        this.authOptions = authOptions.Value;
    }

    public NextStep CreateAuthorizationUri(AuthState state)
    {
        var amrValues = new Dictionary<string, string>
        {
            { "amr_values", authOptions.AmrValues }
        };
        var nemId = new Dictionary<string, Dictionary<string, string>>
        {
            { "nemid", amrValues}
        };

        var query = oidcService.CreateAuthorizationRedirectUrl("code", state, "en");

        query.Add("idp_params", JsonSerializer.Serialize(nemId));

        var authorizationUri = new NextStep { NextUrl = authOptions.OidcUrl + query.ToString() };

        return authorizationUri;
    }
}
