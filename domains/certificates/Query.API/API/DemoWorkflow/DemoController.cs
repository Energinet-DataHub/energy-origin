using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.DemoWorkflow;

[Authorize]
[ApiController]
public class DemoController : ControllerBase
{
    [HttpPost]
    //[ProducesResponseType(typeof(CertificateList), 200)]
    //[ProducesResponseType(204)]
    [Route("api/certificates/demo")]
    public IActionResult Get([FromBody] DemoRequestModel demoRequest) => Accepted();
}

public record DemoRequestModel
{
    public string Foo { get; set; }
}
