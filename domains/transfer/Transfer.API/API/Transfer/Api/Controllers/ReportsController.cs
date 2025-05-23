using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using Asp.Versioning;
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
    private readonly IMediator _mediator;
    private readonly AccessDescriptor _accessDescriptor;

    public ReportsController(
        IMediator mediator,
        AccessDescriptor accessDescriptor)
    {
        _mediator = mediator;
        _accessDescriptor = accessDescriptor;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReportGenerationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Initiates asynchronous report generation.")]
    public IActionResult RequestReportGeneration(
        [FromQuery] Guid organizationId,
        [FromBody] ReportGenerationStartRequest request,
        CancellationToken cancellationToken)
    {
        _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var cmd = new CreateReportRequestCommand(
            ReportId: Guid.NewGuid(),
            OrganizationId: OrganizationId.Create(organizationId),
            StartDate: UnixTimestamp.Create(request.StartDate),
            EndDate: UnixTimestamp.Create(request.EndDate)
        );

        _mediator.Send(cmd, cancellationToken);

        return AcceptedAtAction(
            actionName: null,
            value: new ReportGenerationResponse(cmd.ReportId));
    }

    [HttpGet("{reportId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Downloads the generated report as a file.")]
    public async Task<IActionResult> DownloadReport(
        [FromRoute] Guid reportId,
        [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var result = await _mediator.Send(new DownloadReportCommand(reportId, organizationId), cancellationToken);
        if (result == null || result.Content == null)
            return NotFound();

        return File(result.Content, "application/pdf", $"report-{reportId}.pdf");
    }
}

public record ReportGenerationStartRequest(long StartDate, long EndDate);
public record ReportGenerationResponse(Guid ReportId);
