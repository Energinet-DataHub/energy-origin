using API.Helpers;
using API.Models.Oidc;
using API.Models;
using System.Text.Json;

namespace API.Services;

public class SignaturGruppen : TokenService
{
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
        var authState = new AuthState()
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        var query = CreateAuthorizationRedirectUrl("code", feUrl, authState, "en");

        query.Add("idp_params", JsonSerializer.Serialize(nemId));

        var redirectUrl = new LoginResponse(nextUrl: Configuration.GetOidcUrl() + query.ToString());

        return redirectUrl;
    }
}


