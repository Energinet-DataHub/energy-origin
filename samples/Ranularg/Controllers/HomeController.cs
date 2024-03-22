using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ranularg.Models;

namespace Ranularg.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Secure()
    {
        var claims = User.Claims.ToList();

        if(IsMitIdLogin())
        {
            var accessToken = User.Claims.Single(x => x.Type == "idp_access_token").Value;

            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            string url = "https://pp.netseidbroker.dk/op/connect/userinfo";
            var keyValuePairs = await client.GetFromJsonAsync<Dictionary<string, string>>(url);
            foreach (var pair in keyValuePairs)
            {
                claims.Add(new Claim(pair.Key, pair.Value));
            }
        }

        return View(new SecureViewModel
        {
            Claims = claims
        });
    }

    private bool IsMitIdLogin()
    {
        return User.Claims.SingleOrDefault(x => x.Type == "http://schemas.microsoft.com/identity/claims/identityprovider")
            ?.Value.Equals("https://pp.netseidbroker.dk/op") == true;
    }

    public IActionResult Login()
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, "oidc");
    }

    public IActionResult LoginMitID()
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, "mitid");
    }

    public IActionResult LoginMitIDDirect()
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, "mitiddirect");
    }

    public async Task<IActionResult> Test()
    {
        using var client = new HttpClient();
        var user = HttpContext.User;
        var bearerToken = await HttpContext.GetTokenAsync("access_token");
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var idToken = await HttpContext.GetTokenAsync("id_token");
        var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
        var userinfo = await HttpContext.GetTokenAsync("userinfo");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        var response = await client.GetAsync("http://localhost:5091/secure/weatherforecast");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
        }
        else
        {
            return Error();
        }
    }

    public IActionResult LoginMFA()
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, "mfa");
    }

    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties
        {
            RedirectUri = "/"
        },
            "Cookies", "oidc"
            //"mitid",
            );
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class SecureViewModel
{
    public IEnumerable<Claim> Claims { get; set; }
}
