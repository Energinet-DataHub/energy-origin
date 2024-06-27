using System;
using API;
using EnergyOrigin.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Serilog configuration
        builder.AddSerilogWithoutOutboxLogs();

        // Configure services
        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();

        try
        {
            Log.Information("Starting server.");

            // Configure middleware and endpoints
            startup.Configure(app, builder.Environment);

            app.Run();

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
    }
}
