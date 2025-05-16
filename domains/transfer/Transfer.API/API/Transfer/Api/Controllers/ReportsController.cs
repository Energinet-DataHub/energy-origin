using System;
using System.Collections.Generic;
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
public class ReportsController(IMediator mediator, AccessDescriptor accessDescriptor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Requests asynchronous report generation.")]
    public async Task<IActionResult> RequestReportGeneration(
        [FromQuery] Guid organizationId,
        [FromBody] ReportGenerationStoredApiResponse request,
        CancellationToken cancellationToken)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var cmd = new CreateReportRequestCommand(
            OrganizationId.Create(organizationId),
            UnixTimestamp.Create(request.StartDate),
            UnixTimestamp.Create(request.EndDate)
        );

        var reportId = await mediator.Send(cmd, cancellationToken);

        return AcceptedAtAction(
            actionName: nameof(GetReportStatuses),
            routeValues: new { organizationId },
            value: new { reportId }
        );
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetReportStatusesQueryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Lists all report statuses for an organization.")]
    public async Task<IActionResult> GetReportStatuses(
        [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var query = new GetReportStatusesQuery(OrganizationId.Create(organizationId));
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}

public record ReportGenerationStoredApiResponse(long StartDate, long EndDate);
public record ReportStatusApiResponse(Guid Id, long CreatedAt, ReportStatus Status);
