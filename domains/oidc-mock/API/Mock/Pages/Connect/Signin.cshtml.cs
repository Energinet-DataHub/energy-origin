using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oidc.Mock.Extensions;
using Oidc.Mock.Models;

namespace Oidc.Mock.Pages.Connect;

public class SigninModel : PageModel
{
    private readonly ILogger<SigninModel> logger;
    private readonly ClientCollection clientCollection;

    public User[] Users { get; }

    [FromForm]
    public string? Name { get; set; }

    [FromQuery(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromQuery(Name = "state")]
    public string? State { get; set; }

    public SigninModel(User[] users, ClientCollection clientCollection, ILogger<SigninModel> logger)
    {
        this.clientCollection = clientCollection;
        this.logger = logger;
        Users = users;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        logger.LogInformation("OnPost: ClientId={ClientId}, Name={Name}, RedirectUri={RedirectUri}, State={State}", ClientId, Name, RedirectUri, State);

        var (isValid, validationError) = clientCollection.Validate(ClientId, RedirectUri);
        if (!isValid)
        {
            return BadRequest(validationError);
        }

        var userDescriptor = Users.FirstOrDefault(u => string.Equals(u.Name, Name, StringComparison.InvariantCultureIgnoreCase));
        if (userDescriptor == null)
        {
            logger.LogError("OnPost: User '{Name}' not found", Name);
            return BadRequest($"User '{Name}' not found");
        }

        var code = userDescriptor.Subject?.ToMd5() ?? "";

        var builder = new UriBuilder(RedirectUri ?? "");
        builder.Query = QueryString
            .FromUriComponent(builder.Uri)
            .Add("code", code)
            .Add("state", State ?? "")
            .ToString();

        var uri = builder.ToString();
        logger.LogInformation("Login success: Name={Name}", Name);

        return Redirect(uri);
    }
}
