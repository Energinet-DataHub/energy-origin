using Mock.Oidc;
using Mock.Oidc.Extensions;
using Mock.Oidc.Jwt;
using Mock.Oidc.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddFromYamlFile<UserDescriptor[]>(builder.Configuration["USERS_FILE_PATH"]);
builder.Services.AddSingleton(_ => 
    new ClientDescriptor(
        builder.Configuration["CLIENT_ID"], 
        builder.Configuration["CLIENT_SECRET"], 
        builder.Configuration["CLIENT_REDIRECT_URI"]));

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/health");

app.UseMiddleware<LogFailedRequestMiddleware>();

app.Run();

//Make this a partial class in order to reference it in test project
public partial class Program { }
