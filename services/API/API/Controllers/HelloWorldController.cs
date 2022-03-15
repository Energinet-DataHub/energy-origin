using Microsoft.AspNetCore.Mvc;

namespace DataSync.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloWorldController : ControllerBase
    {
        [HttpGet]
        [Route("Pasta")]
        public string GetPasta()
        {
            return "Pasta";
        }

        [HttpGet]
        [Route("Fries")]
        public string GetFries(int numberOfFries)
        {
            return numberOfFries + " fries";
        }
    }
}
