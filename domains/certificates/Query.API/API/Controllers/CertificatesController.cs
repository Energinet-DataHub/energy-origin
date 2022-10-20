using System;
using System.Linq;
using API.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class CertificatesController : ControllerBase
{
    [HttpGet]
    [Route("v1/certificates")]
    public ActionResult<CertificateList> Get()
    {
        var gsrns = new[]
        {
            "123456789000000000",
            "987654321000000000",
            "112233445566778899"
        };

        var now = DateTimeOffset.Now;
        var timestamp = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);

        var random = new Random();

        var certificates = Enumerable.Range(1, 24)
            .SelectMany(hour =>
                gsrns.Select(gsrn => new Certificate
                {
                    DateFrom = timestamp.AddHours(-hour - 1).ToUnixTimeSeconds(),
                    DateTo = timestamp.AddHours(-hour).ToUnixTimeSeconds(),
                    Quantity = random.Next(1000, 10000),
                    GSRN = gsrn
                }));

        return new CertificateList { Result = certificates.ToList() };
    }
}
