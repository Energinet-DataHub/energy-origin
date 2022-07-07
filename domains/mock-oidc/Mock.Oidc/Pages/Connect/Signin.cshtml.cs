using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mock.Oidc.Pages.Connect;

using Mock.Oidc.Models;

public class SigninModel : PageModel
{
    private readonly ClientDescriptor _client;

    public UserDescriptor[] Users { get; }

    [FromForm]
    public string? Subject { get; set; }

    [FromQuery(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromQuery(Name = "state")]
    public string? State { get; set; }

    public SigninModel(UserDescriptor[] users, ClientDescriptor client)
    {
        _client = client;
        Users = users;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost(string? returnUrl = null)
    {
        if (!string.Equals(ClientId, _client.ClientId, StringComparison.InvariantCultureIgnoreCase))
            return BadRequest("Invalid client_id");

        if (!string.Equals(RedirectUri, _client.RedirectUri, StringComparison.InvariantCultureIgnoreCase))
            return BadRequest("Invalid redirect_uri");

        var userDescriptor = Users.FirstOrDefault(u => u.Subject == Subject);
        if (userDescriptor == null)
        {
            return BadRequest();
        }

        string code = "foo";

        var builder = new UriBuilder(RedirectUri);

        var queryString = QueryString
            .FromUriComponent(builder.Uri)
            .Add("code", code)
            .Add("state", State);
        builder.Query = queryString.ToString();

        var uri = builder.ToString();

        return Redirect(uri);
    }
}