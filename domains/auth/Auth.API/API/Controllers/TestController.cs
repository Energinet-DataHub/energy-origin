using API.Options;
using API.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class TestController : ControllerBase
{
    [HttpGet()]
    [Route("auth/test")]
    public async Task<IActionResult> LoginAsync()
    {
        return Ok("hehe");
    }
}
