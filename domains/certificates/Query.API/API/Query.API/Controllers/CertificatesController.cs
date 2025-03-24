using System;
using System.Threading.Tasks;
using API.Reports;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

[Authorize(Policy = Policy.FrontendOr3rdParty)]
[ApiController]
[ApiVersion(ApiVersions.Version1)]
[ApiExplorerSettings(IgnoreApi = true)]
public class CertificatesController : ControllerBase
{
    private readonly AccessDescriptor _accessDescriptor;
    private readonly CertificatesSpreadsheetExporter _exporter;

    public CertificatesController(AccessDescriptor accessDescriptor, CertificatesSpreadsheetExporter exporter)
    {
        _accessDescriptor = accessDescriptor;
        _exporter = exporter;
    }

    [HttpGet]
    [Route("api/certificates/spreadsheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetWorkItemsAsSpreadSheet(Guid organizationId)
    {
        if (!_accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            return Forbid();
        }
        var spreadsheet = await _exporter.Export(organizationId, HttpContext.RequestAborted);
        return base.File(spreadsheet.Bytes, "application/octet-stream", spreadsheet.Filename);
    }
}
