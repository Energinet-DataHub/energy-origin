using API.Helpers;
using API.Models.Oidc;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;


namespace API.services.SignaturGruppen;


public class SignaturGruppen
{
    public string CreateRedirecthUrl(string feUrl, string returnUrl, string language = "en")
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
        var authState = new AuthState
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        
        var gg = new Jwt



        return "lol";
    }


}


