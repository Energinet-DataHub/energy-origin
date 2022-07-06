using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Mock.Oidc.Pages.Connect;

public class AuthorizeModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest()!;

        // Retrieve the user principal stored in the authentication cookie.
        // If a max_age parameter was provided, ensure that the cookie is not too old.
        // If the user principal can't be extracted or the cookie is too old, redirect the user to the login page.
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        var cookieTooOld = request.MaxAge != null && result.Properties?.IssuedUtc != null && DateTimeOffset.UtcNow - result.Properties.IssuedUtc > TimeSpan.FromSeconds(request.MaxAge.Value);
        if (!result.Succeeded || cookieTooOld)
        {
            // If the client application requested promptless authentication,
            // return an error indicating that the user is not logged in.
            if (request.HasPrompt(Prompts.None))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
                    }));
            }

            return Challenge(
                authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        // If prompt=login was specified by the client application,
        // immediately return the user agent to the login page.
        if (request.HasPrompt(Prompts.Login))
        {
            // To avoid endless login -> authorization redirects, the prompt=login flag
            // is removed from the authorization request payload before redirecting the user.
            var prompt = string.Join(" ", request.GetPrompts().Remove(Prompts.Login));

            var parameters = Request.HasFormContentType ?
                Request.Form.Where(parameter => parameter.Key != Parameters.Prompt).ToList() :
                Request.Query.Where(parameter => parameter.Key != Parameters.Prompt).ToList();

            parameters.Add(KeyValuePair.Create(Parameters.Prompt, new StringValues(prompt)));

            return Challenge(
                authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(parameters)
                });
        }

        // Create a new ClaimsIdentity containing the claims that
        // will be used to create an id_token, a token or a code.
        var identity = new ClaimsIdentity(result.Principal!.Claims,
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name, Claims.Role);

        // The OP-Req-acr_values test consists in sending an "acr_values=1 2" parameter
        // as part of the authorization request. To indicate to the certification client
        // that the "1" reference value was satisfied, an "acr" claim is added.
        if (request.HasAcrValue("1")) //TODO: Is this needed
        {
            identity.AddClaim(new Claim(Claims.AuthenticationContextReference, "1"));
        }

        var principal = new ClaimsPrincipal(identity);

        principal.SetScopes(request.GetScopes());

        // All claims are set to two destinations: access_token and id_token
        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(ImmutableArray.Create(Destinations.AccessToken, Destinations.IdentityToken));
        }

        // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}