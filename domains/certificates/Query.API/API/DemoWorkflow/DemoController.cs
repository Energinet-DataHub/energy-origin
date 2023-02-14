using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.DemoWorkflow;

[Authorize]
[ApiController]
public class DemoController : ControllerBase
{
    [HttpPost]
    //[ProducesResponseType(typeof(CertificateList), 200)] //TODO
    //[ProducesResponseType(204)]
    [Route("api/certificates/demo")]
    public async Task<IActionResult> CreateDemo(
        [FromBody] DemoRequestModel demoRequest,
        [FromServices] IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        var @event = new DemoRequested
        {
            CorrelationId = Guid.NewGuid(), //TODO: Try replacing this with CorrelatedBy<Guid> interface on DemoRequested 
            Foo = demoRequest.Foo
        };
        await publishEndpoint.Publish(@event, cancellationToken);

        return Accepted($"api/certificates/demo/status/{@event.CorrelationId}");
    }

    [HttpGet]
    [Route("api/certificates/demo/status/{correlationId}")]
    public IActionResult GetDemoStatus(
        [FromRoute] Guid correlationId)
    {
        return StatusCode(418); // I'm a teapot
    }
}

public record DemoRequestModel
{
    public string Foo { get; init; } = "";
}
