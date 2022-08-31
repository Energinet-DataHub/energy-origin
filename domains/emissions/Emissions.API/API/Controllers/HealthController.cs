using API.Helpers;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : Controller
{
    private readonly ILogger<HealthController> logger;

    public HealthController(ILogger<HealthController> logger) => this.logger = logger;

    [HttpGet]
    public IActionResult GetStatus()
    {
        try
        {
            Configuration.GetDataSyncEndpoint();
            return StatusCode(200);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get DataSyncEndpoint");
            return StatusCode(500, ex.Message);
        }
    }
}
