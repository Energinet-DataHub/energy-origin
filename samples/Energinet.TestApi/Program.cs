using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Audience = "529a55d0-68c7-4129-ba3c-e06d4f1038c4";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            //ValidIssuer = "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0",
            //ValidAudience = "529a55d0-68c7-4129-ba3c-e06d4f1038c4",
            ValidateIssuer = false
        };

        //options.Authority = "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2";
        options.Authority = "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/B2C_1_SUSI";
    })
    .AddJwtBearer("SecondJwtScheme", options =>
    {
        options.Audience = "529a55d0-68c7-4129-ba3c-e06d4f1038c4";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            //ValidIssuer = "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0",
            //ValidAudience = "529a55d0-68c7-4129-ba3c-e06d4f1038c4",
            ValidateIssuer = false
        };

        options.Authority = "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2";
        //options.Authority = "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/B2C_1_SUSI";
    });


builder.Services.AddAuthorization(
    options =>
    {
        var multi = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "SecondJwtScheme")
            .Build();

        options.AddPolicy("MultiAuthSchemes", multi);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.MapGet("/secure/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetSecureWeatherForecast")
    .RequireAuthorization("MultiAuthSchemes")
    .WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
//diff
