using API.Extensions;
using Microsoft.AspNetCore.Builder;
using Serilog;
using System;

var configuration = WebApplication.CreateBuilder(args).Configuration;


Log.Logger = configuration.GetSeriLogger();

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
