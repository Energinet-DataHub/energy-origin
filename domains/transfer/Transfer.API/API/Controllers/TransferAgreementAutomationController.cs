using System.Threading.Tasks;
using API.ApiModels.Responses;
using API.TransferAgreementsAutomation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [ProducesResponseType(typeof(TransferAutomationStatus), 200)]
    [HttpGet("api/transfer-automation/status")]
    public async Task<ActionResult<TransferAutomationStatus>> GetStatus()
    {
        cache.Cache.TryGetValue(CacheValues.Key, out var value);

        return Ok(value == null ?
            new TransferAutomationStatus(CacheValues.Error) :
            new TransferAutomationStatus(value.ToString()!)
        );
    }
}
