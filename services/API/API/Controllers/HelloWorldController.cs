using Microsoft.AspNetCore.Mvc;

namespace DataSync.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloWorldController : ControllerBase
    {
        //private readonly ILogger<HelloWorldController> _logger;

        //public HelloWorldController(ILogger<HelloWorldController> logger)
        //{
        //    _logger = logger;
        //}

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
