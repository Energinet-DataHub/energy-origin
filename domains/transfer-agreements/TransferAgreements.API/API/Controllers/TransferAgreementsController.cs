using System;
using System.Linq;
using API.ApiModels.Responses;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class TransferAgreementsController : ControllerBase
{
    private static readonly string[] summaries = new[]
    {
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
}
