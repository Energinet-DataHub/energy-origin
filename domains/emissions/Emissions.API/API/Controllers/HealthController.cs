using API.Helpers;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : Controller
    {
        readonly ILogger _logger;

        public HealthController(ILogger logger)
        {
            _logger = logger;
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
                _logger.LogError(ex, null);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
