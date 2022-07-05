using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using System.Globalization;
using System.Security.Claims;

namespace Mock.Oidc.Pages.Connect;

using Mock.Oidc.Models;

public class SigninModel : PageModel
{
    public UserDescriptor[] Users { get; }

    [FromForm]
    public string? Subject { get; set; }

    public SigninModel(UserDescriptor[] users)
    {
        Users = users;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost(string? returnUrl = null)
    {
        var userDescriptor = Users.FirstOrDefault(u => u.Subject == Subject);
        if (userDescriptor == null)
        {
            return BadRequest();
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);

        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.AuthenticationTime, time, ClaimValueTypes.Integer64));
        identity.AddClaims(userDescriptor.ToClaims());

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/connect/signin"
        };

        return SignIn(new ClaimsPrincipal(identity), properties, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}