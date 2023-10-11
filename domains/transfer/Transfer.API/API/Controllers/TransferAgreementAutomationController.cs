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
    private readonly AutomationCache cache;

    public TransferAgreementAutomationController(AutomationCache cache)
    {
        this.cache = cache;
    }

    [ProducesResponseType(typeof(TransferAutomationStatus), 200)]
    [HttpGet("api/transfer-automation/status")]
    public async Task<ActionResult<TransferAutomationStatus>> GetStatus()
    {
        return await Task.Run(() => {
            cache.Cache.TryGetValue(HealthEntries.Key, out var value);

            return Ok(value == null ?
                new TransferAutomationStatus(Healthy: HealthEntries.Unhealthy) :
                new TransferAutomationStatus(Healthy: (bool)value)
            );
        });
    }
}
