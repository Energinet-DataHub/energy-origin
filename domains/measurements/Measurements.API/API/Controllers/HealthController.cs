using API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : Controller
    {
        readonly ILogger<HealthController> logger;

        public HealthController(ILogger<HealthController> logger)
        {
            this.logger = logger;
        }

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
                logger.LogError(ex, null);
                Log.Information("The error message {Message}", ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
