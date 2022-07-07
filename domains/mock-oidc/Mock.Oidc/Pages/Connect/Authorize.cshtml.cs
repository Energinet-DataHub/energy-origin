using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mock.Oidc.Models;

namespace Mock.Oidc.Pages.Connect;

public class AuthorizeModel : PageModel
{
    private readonly ClientDescriptor _client;

    public AuthorizeModel(ClientDescriptor client)
    {
        _client = client;
    }

    public IActionResult OnGet()
    {
        var clientId = Request.Query["client_id"].FirstOrDefault();
        if (!string.Equals(clientId, _client.ClientId, StringComparison.InvariantCultureIgnoreCase))
            return BadRequest("Invalid client_id");

        var redirectUri = Request.Query["redirect_uri"].FirstOrDefault();
        if (!string.Equals(redirectUri, _client.RedirectUri, StringComparison.InvariantCultureIgnoreCase))
            return BadRequest("Invalid redirect_uri");

        //TODO: Transfer all query parameters
        return RedirectToPage("/Connect/Signin", new { state = Request.Query["state"].FirstOrDefault(), client_id = clientId, redirect_uri = redirectUri });
    }
}