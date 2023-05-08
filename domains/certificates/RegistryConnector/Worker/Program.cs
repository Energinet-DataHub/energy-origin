using System;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RegistryConnector.Worker;
using RegistryConnector.Worker.Application;
using RegistryConnector.Worker.Application.EventHandlers;
using RegistryConnector.Worker.Cache;
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

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.RabbitMq));
builder.Services.AddSingleton<ICertificateEventsInMemoryCache, CertificateEventsInMemoryCache>();
builder.Services.RegisterApplication(builder.Configuration);
builder.Services.AddHealthChecks();


builder.Services.AddMassTransit(o =>
{
    o.SetKebabCaseEndpointNameFormatter();

    o.AddConsumer<ProductionCertificateCreatedEventHandler>();

    o.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var url = $"rabbitmq://{options.Host}:{options.Port}";

        cfg.Host(new Uri(url), h =>
        {
            h.Username(options.Username);
            h.Password(options.Password);
        });
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapHealthChecks("/health");
app.RegisterPoEventHandlers();

app.Run();
