using API.Helpers;
using EnergyOriginAuthorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorization.Authorize]
    public class HealthController : Controller
    {
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
                Console.WriteLine(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
