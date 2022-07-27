using API.Helpers;
using API.Models.Oidc;
using Microsoft.AspNetCore.Authentication.OAuth;


namespace API.services.SignaturGruppen;


public class SignaturGruppen
{
    public string CreateRedirecthUrl(string feUrl, string returnUrl)
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

        // /connect/authorize

        // create redirect url
        var oAuthOptions = new OAuthOptions
        {
            AuthorizationEndpoint = $"{Configuration.GetOidcUrl()}/connect/authorize",
        };
        

        var gg = new OAuthEvents();
        var redirectUrl = gg.RedirectToAuthorizationEndpoint(oAuthOptions);
        



        return "lol";
    }


}


