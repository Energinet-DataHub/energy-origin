using System;
using System.Net;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using Asp.Versioning;
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
public class PdfController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AccessDescriptor _accessDescriptor;

    public PdfController(IMediator mediator, AccessDescriptor accessDescriptor)
    {
        _mediator = mediator;
        _accessDescriptor = accessDescriptor;
    }

    public class GeneratePdfRequest
    {
        public string Base64Html { get; set; } = "";
    }

    /// <summary>
    /// Generates a PDF file from a Base64-encoded HTML string.
    /// </summary>
    /// <param name="organizationId">ID of the organization</param>
    /// <param name="request">The request containing the Base64-encoded HTML to be converted into a PDF</param>
    /// <response code="200">PDF file generated successfully</response>
    /// <response code="400">Invalid input</response>
    /// <response code="403">Caller is not authorized to access the specified organization</response>
    [HttpPost]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [Route("api/transfer/pdf/generate")]
    [SwaggerOperation(
        Summary = "Generates a PDF from HTML",
        Description = "Converts the provided base64 encoded HTML into a PDF file"
    )]
    public async Task<IActionResult> Generate([FromQuery] Guid organizationId, [FromBody] GeneratePdfRequest request) // TODO: MASEP Remove body and convert to get request
    {
        _accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var result = await _mediator.Send(new GeneratePdfCommand(request.Base64Html)); // TODO: MASEP Remove bopdy from command

        return result.IsSuccess
            ? File(result.PdfBytes!, "application/pdf")
            : BadRequest();
    }
}
