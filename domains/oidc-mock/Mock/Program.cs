using Microsoft.AspNetCore.Mvc;
using Oidc.Mock;
using Oidc.Mock.Extensions;
using Oidc.Mock.Jwt;
using Oidc.Mock.Models;

var builder = WebApplication.CreateBuilder(args);

// Ignore anti-forgery token on forms - it causes problems when PathBase is used
builder.Services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddFromJsonFile<UserDescriptor[]>(builder.Configuration[Configuration.UsersFilePathKey]);
builder.Services.AddSingleton(_ =>
    new ClientDescriptor(
        builder.Configuration[Configuration.ClientIdKey],
        builder.Configuration[Configuration.ClientSecretKey],
        builder.Configuration[Configuration.ClientRedirectUriKey]));

var app = builder.Build();

// Set PathBase when running behind reverse proxy (see https://www.hanselman.com/blog/dealing-with-application-base-urls-and-razor-link-generation-while-hosting-aspnet-web-apps-behind-reverse-proxies)
if (!string.IsNullOrWhiteSpace(builder.Configuration["PATH_BASE"]))
{
    app.Use((context, next) =>
    {
        context.Request.PathBase = new PathString(builder.Configuration["PATH_BASE"]);
        return next(context);
    });
}

// This middleware is used to log request/responses. With app.UseHttpLogging() it is not possible
// to filter requests to health controller. Can be replaced by Serilog logging at a later point.
app.UseMiddleware<RequestLoggerMiddleware>();

app.UseStaticFiles();

app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

//Make this a partial class in order to reference it in test project
public partial class Program { }
