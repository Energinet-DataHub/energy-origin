using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using API.Models.Oidc;
using API.Helpers;

namespace API.Services;
public class OAuthService
{
    public string CreateAuthorizationUrl(AuthState state, string idp)
    {
        var huhu = new Dictionary<string, string>()
        {
            { "state", state.ToString() },
            { "scope", Configuration.GetScopes().ToString() },
            { "language", "en" },
            { "idp_params", idp }
        };



        var oAuthOptions = new OAuthOptions
        {
            AuthorizationEndpoint = $"{Configuration.GetOidcUrl()}/connect/authorize",
            TokenEndpoint = $"{Configuration.GetOidcUrl()}/connect/token",
            ClientId = Configuration.GetOidcClientId(),
            ClientSecret = Configuration.GetOidcClientSecret(),
            StateDataFormat = (ISecureDataFormat<AuthenticationProperties>)huhu
        };

        /*
        var redirectContext = new RedirectContext<OAuthOptions>(oAuthOptions)


        var gg = new OAuthEvents();

        var redirectUrl = gg.RedirectToAuthorizationEndpoint(RedirectContext(oAuthOptions);

        */


        return "lol";
    }
}
