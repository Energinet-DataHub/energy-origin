using System;
using System.Collections.Generic;
using System.Linq;
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

        return new CertificateList
        {
            Result =
                Enumerable.Range(1, 5).Select(index => new Certificate
                    {
                        Start = timestamp.AddHours(-index - 1).ToUnixTimeSeconds(),
                        End = timestamp.AddHours(-index).ToUnixTimeSeconds(),
                        Amount = random.Next(1000, 10000),
                        MeteringPointId = "123456"
                    })
                    .ToList()
        };
    }
}

public class Certificate
{
    /// <summary>
    /// Start timestamp for the certificate in Unix time
    /// </summary>
    public long Start { get; set; }

    /// <summary>
    /// End timestamp for the certificate in Unix time
    /// </summary>
    public long End { get; set; }

    /// <summary>
    /// Amount of energy measured in Wh
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Id for the metering point
    /// </summary>
    public string MeteringPointId { get; set; }
}

public class CertificateList
{
    public List<Certificate> Result { get; set; }
}
