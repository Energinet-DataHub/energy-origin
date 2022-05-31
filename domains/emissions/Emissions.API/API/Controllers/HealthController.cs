using API.Helpers;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : Controller
    {
        readonly ILogger logger;

        public HealthController(ILogger logger)
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
                return StatusCode(500, ex.Message);
            }
        }
    }
}
