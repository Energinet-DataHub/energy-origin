using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        return Page();
    }
}