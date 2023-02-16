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
    [ProducesResponseType(202)]
    [Route("api/certificates/demo")]
    public async Task<IActionResult> CreateDemo(
        [FromBody] DemoRequestModel demoRequest,
        [FromServices] IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        var @event = new DemoRequested
        {
            CorrelationId = Guid.NewGuid(),
            Foo = demoRequest.Foo
        };
        await publishEndpoint.Publish(@event, cancellationToken);

        return Accepted($"api/certificates/demo/status/{@event.CorrelationId}");
    }

    [HttpGet]
    [ProducesResponseType(typeof(DemoStatusResponse), 200)] //TODO: Should DemoStatusResponse be mapped to API class?
    [ProducesResponseType(typeof(void), 404)]
    [Route("api/certificates/demo/status/{correlationId}")]
    public async Task<IActionResult> GetDemoStatus(
        [FromRoute] Guid correlationId,
        [FromServices] IRequestClient<DemoStatusRequest> requestClient)
    {
        var response = await requestClient.GetResponse<DemoStatusResponse, NotFoundResponse>(
            new DemoStatusRequest { CorrelationId = correlationId });

        return response.Is(out Response<DemoStatusResponse>? okResponse)
            ? Ok(okResponse!.Message)
            : NotFound();
    }
}

public record DemoRequestModel
{
    public string Foo { get; init; } = "";
}
