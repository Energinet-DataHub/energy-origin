using API.Mock.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oidc.Mock.Extensions;
using Oidc.Mock.Models;
namespace Oidc.Mock.Pages.Connect;

public class SigninModel : PageModel
{
    private readonly Options options;
    private readonly ILogger<SigninModel> logger;
    private readonly Client client;

    public string LoginLink => $"https://pp.netseidbroker.dk/op/connect/authorize?client_id=0a775a87-878c-4b83-abe3-ee29c720c3e7&response_type=code&scope=openid%20ssn%20userinfo_token%20nemid%20private_to_business%20mitid%20nemlogin&redirect_uri=https%3A%2F%2F{options.Host}%2Fauth%2Foidc%2Fcallback&state=eyJTdGF0ZSI6bnVsbCwiUmVkaXJlY3Rpb25VcmkiOm51bGx9&prompt=login&idp_values=nemid%20mitid%20mitid_erhverv&idp_params=%7B%22nemid%22%3A%20%7B%22amr_values%22%3A%20%22nemid.otp%20nemid.keyfile%22%7D%7D";
    public User[] Users { get; }

    [FromForm]
    public string? Name { get; set; }

    [FromQuery(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromQuery(Name = "state")]
    public string? State { get; set; }

    public SigninModel(User[] users, Client client, ILogger<SigninModel> logger, Options options)
    {
        this.client = client;
        this.logger = logger;
        this.options = options;
        Users = users;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        logger.LogDebug("OnPost: ClientId={ClientId}, Name={Name}, RedirectUri={RedirectUri}", ClientId, Name, RedirectUri);

        var (isValid, validationError) = client.Validate(ClientId, RedirectUri);
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

        var code = userDescriptor.Name.ToMd5() ?? "";

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
