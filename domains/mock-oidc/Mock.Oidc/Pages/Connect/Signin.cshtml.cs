using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mock.Oidc.Extensions;
using Mock.Oidc.Models;

namespace Mock.Oidc.Pages.Connect;

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

    public IActionResult OnPost()
    {
        var (isValid, validationError) = _client.Validate(ClientId, RedirectUri);
        if (!isValid)
        {
            return BadRequest(validationError);
        }
        
        var userDescriptor = Users.FirstOrDefault(u => u.Subject == Subject);
        if (userDescriptor == null)
        {
            return BadRequest();
        }

        var code = userDescriptor.Name?.ToMd5() ?? "";

        var builder = new UriBuilder(RedirectUri ?? "");
        builder.Query = QueryString
            .FromUriComponent(builder.Uri)
            .Add("code", code)
            .Add("state", State ?? "")
            .ToString();

        var uri = builder.ToString();

        return Redirect(uri);
    }
}