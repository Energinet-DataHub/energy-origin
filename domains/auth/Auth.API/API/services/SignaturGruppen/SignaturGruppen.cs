using API.Helpers;
using API.Models;
using System.Text.Json;

namespace API.Services;

public class SignaturGruppen : ISignaturGruppen
{
    readonly ILogger<SignaturGruppen> logger;
    readonly ITokenService tokenService;

    public SignaturGruppen(ILogger<SignaturGruppen> logger, ITokenService tokenService)
    {
        this.logger = logger;
        this.tokenService = tokenService;
    }

    public LoginResponse CreateRedirecthUrl(string feUrl, string returnUrl)
    {
        var amrValues = new Dictionary<string, string>()
        {
            { "amr_values", Configuration.GetAmrValues() }
        };
        var nemId = new Dictionary<string, Dictionary<string, string>>()
        {
            { "nemid", amrValues}
        };

        // Create dataclass of AuthState
        var authState = new AuthState(feUrl, returnUrl);


        var query = tokenService.CreateAuthorizationRedirectUrl("code", feUrl, authState, "en");

        query.Add("idp_params", JsonSerializer.Serialize(nemId));

        var redirectUrl = new LoginResponse(nextUrl: Configuration.GetOidcUrl() + query.ToString());

        return redirectUrl;
    }
}
