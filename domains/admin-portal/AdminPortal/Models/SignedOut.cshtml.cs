using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminPortal.Models;

[AllowAnonymous]
public class SignedOut : PageModel
{
    public void OnGet()
    {

    }
}
