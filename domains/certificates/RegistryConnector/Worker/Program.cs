using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RegistryConnector.Worker;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var loggerConfiguration = new LoggerConfiguration()
    .Filter
    .ByExcluding("RequestPath like '/health%'");

loggerConfiguration = builder.Environment.IsDevelopment()
    ? loggerConfiguration.WriteTo.Console()
    : loggerConfiguration.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration.CreateLogger());

builder.Services.Configure<RegistryOptions>(builder.Configuration.GetSection(RegistryOptions.Registry));
builder.Services.AddHostedService<Worker>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
