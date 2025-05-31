using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Controllers.HttpUtilities;
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
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IMediator mediator,
        AccessDescriptor accessDescriptor,
        IdentityDescriptor identityDescriptor,
        IServiceScopeFactory scopeFactory,
        ILogger<ReportsController> logger)
    {
        _mediator = mediator;
        _accessDescriptor = accessDescriptor;
        _identityDescriptor = identityDescriptor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReportGenerationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Initiates asynchronous report generation.")]
    public IActionResult RequestReportGeneration(
        [FromQuery] Guid organizationId,
        [FromBody] ReportGenerationStartRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting report generation request for organization {OrganizationId}", organizationId);

        try
        {
            _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);
            _logger.LogDebug("Authorization check passed for organization {OrganizationId}", organizationId);

            var reportId = Guid.NewGuid();
            _logger.LogInformation("Created new report with ID {ReportId} for organization {OrganizationId}", reportId, organizationId);

            var cmd = new CreateReportRequestCommand(
                ReportId: reportId,
                OrganizationId: OrganizationId.Create(organizationId),
                OrganizationName: OrganizationName.Create(_identityDescriptor.OrganizationName),
                OrganizationTin: Tin.Create(_identityDescriptor.OrganizationCvr ?? throw new BusinessException("Organization CVR is missing")),
                StartDate: UnixTimestamp.Create(request.StartDate),
                EndDate: UnixTimestamp.Create(request.EndDate),
                Language: AcceptLanguageParser.GetPreferredLanguage(Request.Headers));

            _logger.LogDebug("Created report command with date range: {StartDate} to {EndDate}", request.StartDate, request.EndDate);

            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background processing for report {ReportId}", cmd.ReportId);
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    await mediator.Send(cmd, CancellationToken.None);
                    _logger.LogInformation("Successfully completed background processing for report {ReportId}", cmd.ReportId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during background processing of report {ReportId}", cmd.ReportId);
                }
            });

            _logger.LogInformation("Report generation request accepted for report {ReportId}", cmd.ReportId);
            return AcceptedAtAction(
                actionName: null,
                value: new ReportGenerationResponse(cmd.ReportId));
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business exception during report generation request for organization {OrganizationId}: {Message}",
                organizationId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during report generation request for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetReportStatusesQueryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Lists all report statuses for an organization.")]
    public async Task<IActionResult> GetReportStatuses(
        [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting report statuses for organization {OrganizationId}", organizationId);

        try
        {
            _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);
            _logger.LogDebug("Authorization check passed for organization {OrganizationId}", organizationId);

            var query = new GetReportStatusesQuery(OrganizationId.Create(organizationId));
            _logger.LogDebug("Executing GetReportStatusesQuery for organization {OrganizationId}", organizationId);

            var result = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("Successfully retrieved {Count} report statuses for organization {OrganizationId}",
                result?.Result?.Count ?? 0, organizationId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report statuses for organization {OrganizationId}", organizationId);
            throw;
        }
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
        _logger.LogInformation("Download request for report {ReportId} from organization {OrganizationId}", reportId, organizationId);

        try
        {
            _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);
            _logger.LogDebug("Authorization check passed for organization {OrganizationId}", organizationId);

            _logger.LogDebug("Executing DownloadReportCommand for report {ReportId}", reportId);
            var result = await _mediator.Send(new DownloadReportCommand(reportId, organizationId), cancellationToken);

            if (result == null || result.Content == null)
            {
                _logger.LogWarning("Report {ReportId} not found or has no content for organization {OrganizationId}",
                    reportId, organizationId);
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved report {ReportId} for download, content size: {ContentSize} bytes",
                reportId, result.Content.Length);
            return File(result.Content, "application/pdf", $"report-{reportId}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading report {ReportId} for organization {OrganizationId}", reportId, organizationId);
            throw;
        }
    }
}

public record ReportGenerationStartRequest(long StartDate, long EndDate);
public record ReportGenerationResponse(Guid ReportId);
