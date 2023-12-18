using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TransferAgreementAutomation.Worker.Api;

[Authorize]
[ApiController]
public class TransferAutomationController : ControllerBase
{
    private readonly AutomationCache cache;

    public TransferAutomationController(AutomationCache cache)
    {
        this.cache = cache;
    }

    [ProducesResponseType(typeof(TransferAutomationStatus), 200)]
    [HttpGet("api/transfer-automation/status")]
    public async Task<ActionResult<TransferAutomationStatus>> GetStatus()
    {
        return await Task.Run(() =>
        {
            cache.Cache.TryGetValue(HealthEntries.Key, out var value);

            return Ok(value == null ?
                new TransferAutomationStatus(Healthy: HealthEntries.Unhealthy) :
                new TransferAutomationStatus(Healthy: (bool)value)
            );
        });
    }
}
