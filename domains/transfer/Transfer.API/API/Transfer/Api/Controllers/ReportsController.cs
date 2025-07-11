using API.Transfer.Api._Features_;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;

namespace API.Transfer.Api.Controllers;

[ApiController]
[Authorize(Policy = Policy.FrontendOr3rdParty)]
[ApiVersion(ApiVersions.Version1)]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AccessDescriptor _accessDescriptor;
    private readonly IdentityDescriptor _identityDescriptor;
    private readonly IServiceScopeFactory _scopeFactory;

    public ReportsController(
        IMediator mediator,
        IUnitOfWork unitOfWork,
        AccessDescriptor accessDescriptor,
        IdentityDescriptor identityDescriptor,
        IServiceScopeFactory scopeFactory)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _accessDescriptor = accessDescriptor;
        _identityDescriptor = identityDescriptor;
        _scopeFactory = scopeFactory;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReportGenerationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Initiates asynchronous report generation.")]
    public async Task<IActionResult> RequestReportGeneration(
        [FromServices] IValidator<ReportGenerationStartRequest> validator,
        [FromQuery] Guid organizationId,
        [FromBody] ReportGenerationStartRequest request,
        CancellationToken cancellationToken)
    {
        _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var tin = Tin.TryParse(_identityDescriptor.OrganizationCvr);
        if (tin == null)
            return BadRequest("Organization must have a valid tin.");

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var report = Report.Create(
            id: Guid.NewGuid(),
            organizationId: OrganizationId.Create(organizationId),
            organizationName: OrganizationName.Create(_identityDescriptor.OrganizationName),
            organizationTin: tin,
            orgStatus: _identityDescriptor.OrganizationStatus,
            startDate: UnixTimestamp.Create(request.StartDate),
            endDate: UnixTimestamp.Create(request.EndDate));

        await _unitOfWork.ReportRepository.AddAsync(report, cancellationToken);
        await _unitOfWork.SaveAsync();

        var cmd = new PopulateReportCommand(
            ReportId: report.Id
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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
