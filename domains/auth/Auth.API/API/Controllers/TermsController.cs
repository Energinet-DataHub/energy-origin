using API.Models;
using API.Repository;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class TermsController : ControllerBase
{
    private readonly IPrivacyPolicyStorage privacyPolicyStorage;

    public TermsController(IPrivacyPolicyStorage privacyPolicyStorage) =>
        this.privacyPolicyStorage = privacyPolicyStorage;

    [HttpGet]
    [Route("/auth/terms")]
    public async Task<ActionResult<PrivacyPolicy>> Get()
    {
        try
        {
            return Ok(await privacyPolicyStorage.Get());
        }
        catch (Exception e)
        {
            return Problem();
        }
    }
}
