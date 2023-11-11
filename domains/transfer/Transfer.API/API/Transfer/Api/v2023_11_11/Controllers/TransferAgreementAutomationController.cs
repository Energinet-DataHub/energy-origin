using System.Threading.Tasks;
using API.Transfer.Api.v2023_01_01.Dto.Responses;
using API.Transfer.TransferAgreementsAutomation;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.v2023_11_11.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20231111")]
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
