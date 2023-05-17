using System;
using System.Linq;
using System.Security.Claims;
using API.ApiModels;
using API.ApiModels.Responses;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TransferAgreementsController : ControllerBase
{
    private static readonly string[] summaries = {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    /// <summary>
    /// This comment is included
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(WeatherForecastList), 200)]
    [Route("api/transfer-agreements")]
    public IActionResult Get() =>
        Ok(new WeatherForecastList
        {
            Result =
                Enumerable.Range(1, 5)
                    .Select(index => new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray()
        });

    /// <summary>
    /// An example of how to get the UUID for the signed in user
    /// </summary>
    [HttpGet]
    [Route("api/transfer-agreements/subject")]
    public IActionResult GetSubject() => Ok(User.FindFirstValue(ClaimTypes.NameIdentifier));

    [AllowAnonymous]
    [HttpGet]
    [Route("api/transfer-agreements/test1")]
    public IActionResult Test1([FromServices] ApplicationDbContext context)
    {
        var transferAgreement = new TransferAgreement
        {
            EndDate = DateTimeOffset.UtcNow.AddHours(1),
            StartDate = DateTimeOffset.UtcNow.AddHours(-1),
            ReceiverTin = 42
        };

        context.TransferAgreements.Add(transferAgreement);

        context.SaveChanges();

        return Ok(transferAgreement);
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("api/transfer-agreements/test2/{id}")]
    public IActionResult Test2([FromRoute] Guid id, [FromServices] ApplicationDbContext context)
        => Ok(context.TransferAgreements.Find(id));
}
