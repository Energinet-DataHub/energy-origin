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
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Initiates asynchronous report generation.")]
    public IActionResult RequestReportGeneration(
        [FromQuery] Guid organizationId,
        [FromBody] ReportGenerationStoredApiResponse request,
        CancellationToken cancellationToken)
    {
        _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var cmd = new CreateReportRequestCommand(
            OrganizationId.Create(organizationId),
            UnixTimestamp.Create(request.StartDate),
            UnixTimestamp.Create(request.EndDate)
        );

        _mediator.Send(cmd, cancellationToken);

        return AcceptedAtAction(
            null,
            new { organizationId },
            null);
    }
}

public record ReportGenerationStoredApiResponse(long StartDate, long EndDate);
