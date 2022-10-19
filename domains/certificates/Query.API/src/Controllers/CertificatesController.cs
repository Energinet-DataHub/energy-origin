using Microsoft.AspNetCore.Mvc;

namespace src.Controllers; //TODO: Bad namespace

[ApiController]
public class CertificatesController : ControllerBase
{
    [HttpGet]
    [Route("v1/certificates")]
    public ActionResult<CertificateList> Get()
    {
        var now = DateTimeOffset.Now;
        var n = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);

        var random = new Random(n.Year+n.Month+n.Day+n.Hour);

        return new CertificateList(
            Enumerable.Range(1, 5).Select(index => new Certificate(
                    Start: n.AddHours(-index - 1),
                    End: n.AddHours(-index),
                    AmountWh: random.Next(1000, 10000),
                    MeteringPoint: "123456"))
                .ToList()
        );
    }
}
