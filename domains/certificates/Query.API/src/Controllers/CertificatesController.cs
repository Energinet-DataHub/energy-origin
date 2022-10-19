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
        var timestamp = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);

        var random = new Random();

        return new CertificateList(
            Enumerable.Range(1, 5).Select(index => new Certificate(
                    Start: timestamp.AddHours(-index - 1).ToUnixTimeSeconds(),
                    End: timestamp.AddHours(-index).ToUnixTimeSeconds(),
                    Amount: random.Next(1000, 10000),
                    MeteringPointId: "123456"))
                .ToList()
        );
    }
}

// TODO: Document in swagger that unit for Amount is Wh
public record Certificate(long Start, long End, int Amount, string MeteringPointId);

public record CertificateList(List<Certificate> Result);

