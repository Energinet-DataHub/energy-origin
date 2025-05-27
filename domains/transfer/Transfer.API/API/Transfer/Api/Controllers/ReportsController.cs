using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using Asp.Versioning;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Exceptions;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IdentityDescriptor _identityDescriptor;
    private readonly IServiceScopeFactory _scopeFactory;


    public ReportsController(
        IMediator mediator,
        AccessDescriptor accessDescriptor,
        IdentityDescriptor identityDescriptor,
        IServiceScopeFactory scopeFactory)
    {
        _mediator = mediator;
        _accessDescriptor = accessDescriptor;
        _identityDescriptor = identityDescriptor;
        _scopeFactory      = scopeFactory;
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
            OrganizationName: OrganizationName.Create(_identityDescriptor.OrganizationName),
            OrganizationTin: Tin.Create(_identityDescriptor.OrganizationCvr ?? throw new BusinessException("Organization CVR is missing")),
            StartDate: UnixTimestamp.Create(request.StartDate),
            EndDate: UnixTimestamp.Create(request.EndDate)
        );

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(cmd, CancellationToken.None);
        });

        return AcceptedAtAction(
            actionName: null,
            value: new ReportGenerationResponse(cmd.ReportId));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetReportStatusesQueryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Lists all report statuses for an organization.")]
    public async Task<IActionResult> GetReportStatuses(
        [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var query = new GetReportStatusesQuery(OrganizationId.Create(organizationId));
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
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
