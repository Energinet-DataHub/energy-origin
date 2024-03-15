using API.Extensions;
using API.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using EnergyOrigin.Setup;

var configuration = WebApplication.CreateBuilder(args).Configuration;

var otlpConfiguration = configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>();

Log.Logger = LoggerBuilder.BuildSerilogger(otlpOptions!.ReceiverEndpoint);

try
{
    Log.Information("Starting server.");
    WebApplication app = configuration.BuildApp();

    await app.RunAsync();
    Log.Information("Server stopped.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    Environment.ExitCode = -1;
}
finally
{
    Log.CloseAndFlush();
}
