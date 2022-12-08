using API.Models;
using API.Repository;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class TermsController : ControllerBase
{
    private readonly IPrivacyPolicyStorage storage;

    public TermsController(IPrivacyPolicyStorage storage) =>
        this.storage = storage;

    [HttpGet]
    [Route("/auth/terms")]
    public async Task<ActionResult<PrivacyPolicy>> Get()
    {
        try
        {
            return Ok(await storage.GetLatestVersion());
        }
        catch (Exception)
        {
            return Problem();
        }
    }
}
