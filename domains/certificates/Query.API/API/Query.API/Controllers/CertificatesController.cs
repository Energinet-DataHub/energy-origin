using System;
using System.Threading.Tasks;
using API.Reports;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20240423)]
[ApiExplorerSettings(IgnoreApi = true)]
public class CertificatesController : ControllerBase
{
    private readonly CertificatesSpreadsheetExporter _exporter;

    public CertificatesController(CertificatesSpreadsheetExporter exporter)
    {
        _exporter = exporter;
    }

    [HttpGet]
    [Route("api/certificates/spreadsheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetWorkItemsAsSpreadSheet(Guid organizationId)
    {
        var userDescriptor = new UserDescriptor(HttpContext);
        var spreadsheet = await _exporter.Export(userDescriptor.Subject, HttpContext.RequestAborted);
        return base.File(spreadsheet.Bytes, "application/octet-stream", spreadsheet.Filename);
    }
}
