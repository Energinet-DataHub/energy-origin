using System;
using System.Linq;
using API.Extensions;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Migrations;
using EnergyOrigin.Setup.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (args.Contains("--migrate"))
{
    builder.AddSerilogWithoutOutboxLogs();
    var migrateApp = builder.Build();
    var dbMigrator = new DbMigrator(builder.Configuration.GetConnectionString("Postgres")!, typeof(Program).Assembly,
        migrateApp.Services.GetRequiredService<ILogger<DbMigrator>>());
    await dbMigrator.MigrateAsync();
    return;
}

var configuration = builder.Configuration;
Log.Logger = LoggerBuilder.BuildSerilogger();

try
{
    Log.Information("Starting server.");
    WebApplication app = configuration.BuildApp(args);

    if (args.Contains("--swagger"))
    {
        app.BuildSwaggerYamlFile(app.Environment, "measurements.yaml"); // Lets build
    }
    else
    {
        await app.RunAsync();
        Log.Information("Server stopped.");
    }
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
