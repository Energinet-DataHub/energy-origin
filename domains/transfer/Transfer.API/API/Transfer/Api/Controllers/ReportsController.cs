using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Transfer.Api.Controllers;

[ApiController]
[Authorize(Policy = Policy.FrontendOr3rdParty)]
[ApiVersion(ApiVersions.Version1)]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator         _mediator;
    private readonly AccessDescriptor _accessDescriptor;

    public ReportsController(
        IMediator          mediator,
        AccessDescriptor   accessDescriptor)
    {
        _mediator = mediator;
        _accessDescriptor   = accessDescriptor;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Initiates asynchronous report generation.")]
    public async Task<IActionResult> RequestReportGeneration(
        [FromQuery] Guid                         organizationId,
        [FromBody]  ReportGenerationStoredApiResponse request,
        CancellationToken                        cancellationToken)
    {
        _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var cmd = new CreateReportRequestCommand(
            OrganizationId.Create(organizationId),
            UnixTimestamp.Create(request.StartDate),
            UnixTimestamp.Create(request.EndDate)
        );

        var reportId = await _mediator.Send(cmd, cancellationToken);

        return AcceptedAtAction(
            nameof(GetReportStatus),
            new { organizationId, reportId },
            null
        );
    }

    [HttpGet("{reportId}")]
    [ProducesResponseType(typeof(ReportStatusApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Gets the status of a specific report.")]
    public Task<IActionResult> GetReportStatus(
        [FromQuery] Guid organizationId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        throw new NotImplementedException();
    }
}

public record ReportGenerationStoredApiResponse(long StartDate, long EndDate);
public record ReportStatusApiResponse(Guid Id, long CreatedAt, ReportStatus Status);
