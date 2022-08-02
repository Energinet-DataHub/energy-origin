using API.Services;
using Serilog;
using Serilog.Formatting.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("Tests")]

var logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

builder.Services.AddHttpClient();
builder.Services.AddScoped<ISignaturGruppen, SignaturGruppen>();

var app = builder.Build();

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
/*
void ConfigureServices(IServiceCollection services)
{
    services.AddAuthentication(config =>
    {
        // Check cookie for authentication
        config.DefaultAuthenticateScheme = "ClientCookie";
        // Deal out cookie
        config.DefaultSignInScheme = "ClientCookie";
        // Check if authenticated
        config.DefaultChallengeScheme = "Oidc";
    })
        .AddCookie("ClientCookie")
        .AddOAuth("Oidc", config =>
        {
            config.ClientId = Configuration.GetOidcClientId();
            config.ClientSecret = Configuration.GetOidcClientSecret();
            config.CallbackPath = "/oidc/callback";
            config.AuthorizationEndpoint = $"{Configuration.GetOidcUrl}/connect/authorize";
        });
}
*/



app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
