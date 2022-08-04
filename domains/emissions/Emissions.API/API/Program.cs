using API.Helpers;
using API.Models;
using FluentValidation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Serilog;
using Serilog.Formatting.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

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
if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
