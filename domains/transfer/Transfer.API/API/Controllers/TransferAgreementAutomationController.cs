using System.Threading.Tasks;
using API.ApiModels.Responses;
using API.TransferAgreementsAutomation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Trace;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TransferAgreementAutomationController : ControllerBase
{
    private readonly StatusCache cache;

    public TransferAgreementAutomationController(StatusCache cache)
    {
        this.cache = cache;
    }

    [HttpGet("transfer-automation/status")]
    public async Task<ActionResult<TransferAutomationStatus>> GetStatus()
    {
        cache.Cache.TryGetValue(CacheValues.Key, out var value);

        return Ok(value == null ?
            new TransferAutomationStatus(CacheValues.Error) :
            new TransferAutomationStatus(value.ToString())
        );
    }
}
