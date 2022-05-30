using API.DataSync.Requests;
using API.DataSync.Responses;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using EnergyOriginAuthorization;
using API.Helpers;


namespace DataSync.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorization.Authorize]
    public class EmissionsController : ControllerBase
    {

        [HttpGet]
        [Route("emissions/{gsrn}/measurements")]
        public async Task<IEnumerable<Emissions>> MeterTimeSeries(string subject, long gsrn, long dateFrom, long dateTo)
        {

            var request = new EmissionsRequest(subject, gsrn, dateFrom, dateTo);

            //var measurements = new List<Emissions>();

            return request;

        }
    }
}
