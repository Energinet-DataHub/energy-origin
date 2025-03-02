using API.Mock.Models;
using Microsoft.AspNetCore.Mvc;
using Oidc.Mock;
using Oidc.Mock.Extensions;
using Oidc.Mock.Jwt;
using Oidc.Mock.Models;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var loggerConfiguration = new LoggerConfiguration()
    .Filter.ByExcluding("RequestPath like '/health%'")
    .Filter.ByExcluding("RequestPath like '/metrics%'")
    .Enrich.With(new SerilogSanitizationMiddleware.SanitizeLogEnricher())
    .Destructure.With<SerilogSanitizationMiddleware.SanitizeLogDestructuringPolicy>();

loggerConfiguration = builder.Environment.IsDevelopment()
    ? loggerConfiguration.WriteTo.Console()
    : loggerConfiguration.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration.CreateLogger());

// Ignore anti-forgery token on forms - it causes problems when PathBase is used
builder.Services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddFromJsonFile<User[]>(builder.Configuration[Configuration.UsersFilePathKey]!);
var clients = builder.Configuration.GetSection(Configuration.ClientsPrefix).Get<List<Client>>() ?? new List<Client>();
builder.Services.AddSingleton(new ClientCollection(clients));
builder.Services.AddSingleton(x => new Options(Host: builder.Configuration[Configuration.Host]!));

var app = builder.Build();

// Set PathBase when running behind reverse proxy (see https://www.hanselman.com/blog/dealing-with-application-base-urls-and-razor-link-generation-while-hosting-aspnet-web-apps-behind-reverse-proxies)
if (!string.IsNullOrWhiteSpace(builder.Configuration[Configuration.PathBaseKey]))
{
    app.Use((context, next) =>
    {
        context.Request.PathBase = new PathString(builder.Configuration[Configuration.PathBaseKey]);
        return next(context);
    });
}

app.UseStaticFiles();

app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

//Make this a partial class in order to reference it in test project
public partial class Program { }
