using System;
using API.Extensions;
using EnergyOrigin.Setup;
using Microsoft.AspNetCore.Builder;
using Serilog;

var configuration = WebApplication.CreateBuilder(args).Configuration;

Log.Logger = LoggerBuilder.BuildSerilogger();

try
{
    Log.Information("Starting server.");
    WebApplication app = configuration.BuildApp(args);

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
