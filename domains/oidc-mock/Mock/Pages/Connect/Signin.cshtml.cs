using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oidc.Mock.Extensions;
using Oidc.Mock.Models;

namespace Oidc.Mock.Pages.Connect;

public class SigninModel : PageModel
{
    private readonly ILogger<SigninModel> logger;
    private readonly Client client;

    public User[] Users { get; }

    [FromForm]
    public string? Name { get; set; }

    [FromQuery(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromQuery(Name = "state")]
    public string? State { get; set; }

    public SigninModel(User[] users, Client client, ILogger<SigninModel> logger)
    {
        this.client = client;
        this.logger = logger;
        Users = users;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        logger.LogDebug($"OnPost: ClientId={ClientId}, Name={Name}, RedirectUri={RedirectUri}");

        var (isValid, validationError) = client.Validate(ClientId, RedirectUri);
        if (!isValid)
        {
            return BadRequest(validationError);
        }

        var userDescriptor = Users.FirstOrDefault(u => string.Equals(u.Name, Name, StringComparison.InvariantCultureIgnoreCase));
        if (userDescriptor == null)
        {
            logger.LogError($"OnPost: User '{Name}' not found");
            return BadRequest($"User '{Name}' not found");
        }

        var code = userDescriptor.Name.ToMd5() ?? "";

        var builder = new UriBuilder(RedirectUri ?? "");
        builder.Query = QueryString
            .FromUriComponent(builder.Uri)
            .Add("code", code)
            .Add("state", State ?? "")
            .ToString();

        var uri = builder.ToString();

        logger.LogInformation($"Login success: Name={Name}");

        return Redirect(uri);
    }
}
