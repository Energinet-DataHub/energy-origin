using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Helpers;
using API.Models;
using FluentValidation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Serilog;
using Serilog.Formatting.Json;

[assembly: InternalsVisibleTo("Tests")]

var logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddValidatorsFromAssemblyContaining<EnergySourceRequest.Validator>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Inform Swagger about FluentValidation rules. See https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation for more details
builder.Services.AddTransient<IValidatorFactory, ServiceProviderValidatorFactory>();
builder.Services.AddFluentValidationRulesToSwagger();

builder.Services.AddHttpClient();
builder.Services.AddCustomServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger(o => o.RouteTemplate = "api-docs/emissions/{documentName}/swagger.json");
if (builder.Environment.IsDevelopment())
{
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/api-docs/emissions/v1/swagger.json", "API v1"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();
