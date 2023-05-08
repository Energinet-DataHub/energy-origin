using System;
using System.Collections.Generic;
using System.Linq;

namespace API.ApiModels.Responses;

public class WeatherForecast
{
    /// <summary>
    /// This is a fine comment
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// This is also a comment
    /// </summary>
    public int TemperatureC { get; set; }

    /// <summary>
    /// Look. Another comment
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    /// <summary>
    /// Wow! I love comments
    /// </summary>
    public string? Summary { get; set; }
}

public class WeatherForecastList
{
    public IEnumerable<WeatherForecast> Result { get; set; } = Enumerable.Empty<WeatherForecast>();
}
