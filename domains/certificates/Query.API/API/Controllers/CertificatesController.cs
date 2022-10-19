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
        var meteringPointIds = new[] { "123456", "654321", "162534" };
        
        var now = DateTimeOffset.Now;
        var timestamp = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);

        var random = new Random();

        var certificates = Enumerable.Range(1, 24)
            .SelectMany(hour =>
                meteringPointIds.Select(mpid => new Certificate
                {
                    Start = timestamp.AddHours(-hour - 1).ToUnixTimeSeconds(),
                    End = timestamp.AddHours(-hour).ToUnixTimeSeconds(),
                    Amount = random.Next(1000, 10000),
                    MeteringPointId = mpid
                }));

        return new CertificateList { Result = certificates.ToList() };
    }
}
