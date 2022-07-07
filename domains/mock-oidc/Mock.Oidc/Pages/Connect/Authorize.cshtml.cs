using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mock.Oidc.Pages.Connect;

public class AuthorizeModel : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }
}