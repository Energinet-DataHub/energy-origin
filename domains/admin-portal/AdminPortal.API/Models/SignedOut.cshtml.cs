using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminPortal.API.Models;

[AllowAnonymous]
public class SignedOut : PageModel
{
    public void OnGet()
    {

    }
}
