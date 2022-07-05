using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using System.Globalization;
using System.Security.Claims;

namespace Mock.Oidc.Pages.Connect;

public class SigninModel : PageModel
{
    [FromForm]
    public string? Username { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost(string? returnUrl = null)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);

        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.AuthenticationTime, time, ClaimValueTypes.Integer64));

        if (string.Equals(Username, "Charlotte", StringComparison.OrdinalIgnoreCase))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, "7DADB7DB-0637-4446-8626-2781B06A9E20"));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, "Charlotte"));
        }
        else if (string.Equals(Username, "Not-Charlotte", StringComparison.OrdinalIgnoreCase))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, "95D7BE81-0CFB-4B52-9C92-33A45747FCEF"));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, "Not Charlotte"));
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/connect/signin"
        };

        return SignIn(new ClaimsPrincipal(identity), properties, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}