using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using Asp.Versioning;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Transfer.Api.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version1)]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Records a request to generate a report.")]
    public async Task<IActionResult> RequestReportGeneration(
        [FromBody] RequestReportDto request,
        CancellationToken cancellationToken)
    {
        var reportId = await _mediator.Send(
            new CreateReportRequestCommand(
                UnixTimestamp.Create(request.StartDate),
                UnixTimestamp.Create(request.EndDate)),
            cancellationToken);

        return AcceptedAtAction(
            nameof(GetReportStatus),
            new { reportId },
            null);
    }

    [HttpGet("{reportId}")]
    public IActionResult GetReportStatus(Guid reportId)
        => throw new NotImplementedException();
}

public record RequestReportDto(long StartDate, long EndDate);
