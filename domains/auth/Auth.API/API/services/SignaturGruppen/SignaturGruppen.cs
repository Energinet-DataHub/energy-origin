using API.Helpers;
using API.Models.Oidc;
using System.Text.Json;


namespace API.Services;


public class SignaturGruppen : OAuthService
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

        JsonSerializer.Serialize(nemId);


        CreateAuthorizationUrl(authState, JsonSerializer.Serialize(nemId));
        
        

        return "lol";
    }


}


