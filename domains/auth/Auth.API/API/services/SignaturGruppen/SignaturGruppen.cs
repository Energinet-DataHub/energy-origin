using API.Helpers;
using API.Models.Oidc;
using API.Services;
using System.Text.Json;


namespace API.Services;


public class SignaturGruppen : TokenService
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

        var query = CreateAuthorizationRedirectUrl("code", authState, "en");

        query.Add("idp_params", JsonSerializer.Serialize(nemId));  

        return query.ToString();
    }
}


