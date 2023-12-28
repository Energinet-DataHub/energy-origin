using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransferAgreementAutomation.Worker.Api.v2023_12_21.Dto;

namespace TransferAgreementAutomation.Worker.Api.v2023_12_21.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20231123")]
public class TransferAutomationController(AutomationCache cache) : ControllerBase
{
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
