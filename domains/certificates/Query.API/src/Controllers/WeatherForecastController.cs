using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

[ApiController]
public class WeatherForecastController : ControllerBase
{
    [HttpGet]
    [Route("v1/certificates")]
    public ActionResult<CertificateList> Get()
    {
        var now = DateTimeOffset.Now;
        var n = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);

        return new CertificateList
        {
            Result = Enumerable.Range(1, 5).Select(index => new Certificate
            {
                Start = n.AddHours(-index - 1),
                End = n.AddHours(-index),
                AmountWh = 42,
                MeteringPoint = "123456"
            }).ToList()
        };
    }
}
