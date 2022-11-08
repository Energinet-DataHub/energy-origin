using System;
using System.Linq;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[Authorize]
[ApiController]
public class CertificatesController : ControllerBase
{
    private readonly ILogger<CertificatesController> logger;

    public CertificatesController(ILogger<CertificatesController> logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    [Route("certificates")]
    public ActionResult<CertificateList> Get()
    {
        var claims = User.Claims
            .Select(c => c.ToString())
            .ToArray();
        logger.LogInformation("User claims {claims}", string.Join(";", claims));
        
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
