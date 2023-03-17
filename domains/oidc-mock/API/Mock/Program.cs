using API.Mock.Models;
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
builder.Services.AddFromJsonFile<User[]>(builder.Configuration[Configuration.UsersFilePathKey]!);
builder.Services.AddSingleton(_ =>
    new Client(
        builder.Configuration[Configuration.ClientIdKey]!,
        builder.Configuration[Configuration.ClientSecretKey]!,
        builder.Configuration[Configuration.ClientRedirectUriKey]!));
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

// This middleware is used to log request/responses. With app.UseHttpLogging() it is not possible
// to filter requests to health controller. Can be replaced by Serilog logging at a later point.
app.UseMiddleware<RequestLoggerMiddleware>();

app.UseStaticFiles();

app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

//Make this a partial class in order to reference it in test project
public partial class Program { }
