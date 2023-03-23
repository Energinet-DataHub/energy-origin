using API.Middleware;
using API.Options;
using API.Repositories;
using API.Repositories.Data;
using API.Services;
using API.Utilities;
using EnergyOrigin.TokenValidation.Utilities;
using IdentityModel.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Json;

var logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

var tokenConfiguration = builder.Configuration.GetSection(TokenOptions.Prefix);
var tokenOptions = tokenConfiguration.Get<TokenOptions>()!;

var databaseConfiguration = builder.Configuration.GetSection(DatabaseOptions.Prefix);
var databaseOptions = databaseConfiguration.Get<DatabaseOptions>()!;

builder.Services.Configure<TokenOptions>(tokenConfiguration);
builder.Services.Configure<DatabaseOptions>(databaseConfiguration);
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.Prefix));
builder.Services.Configure<CryptographyOptions>(builder.Configuration.GetSection(CryptographyOptions.Prefix));
builder.Services.Configure<TermsOptions>(builder.Configuration.GetSection(TermsOptions.Prefix));
builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection(TokenOptions.Prefix));
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.Prefix));

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services.Configure<CryptographyOptions>(builder.Configuration.GetSection(CryptographyOptions.Prefix));
builder.Services.Configure<TermsOptions>(builder.Configuration.GetSection(TermsOptions.Prefix));
builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection(TokenOptions.Prefix));
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.Prefix));

builder.AddTokenValidation(new ValidationParameters(tokenOptions.PublicKeyPem)
{
    ValidAudience = tokenOptions.Audience,
    ValidIssuer = tokenOptions.Issuer
});

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
    {
        new OpenApiSecurityScheme {
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"},
            Scheme = "oauth2",
            Name = "Bearer",
            In = ParameterLocation.Header,
        }, new List<string>() }
    });
});

builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql($"Host={databaseOptions.Host}; Port={databaseOptions.Port}; Database={databaseOptions.Name}; Username={databaseOptions.User}; Password={databaseOptions.Password};"));

builder.Services.AddSingleton<IDiscoveryCache>(providers =>
{
    var options = providers.GetRequiredService<IOptions<OidcOptions>>();
    return new DiscoveryCache(options.Value.AuthorityUri.AbsoluteUri)
    {
        CacheDuration = options.Value.CacheDuration
    };
});
builder.Services.AddSingleton<ICryptography, Cryptography>();
builder.Services.AddSingleton<IUserDescriptMapper, UserDescriptMapper>();
builder.Services.AddSingleton<ITokenIssuer, TokenIssuer>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserDataContext, DataContext>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(provider =>
        provider
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter())
    .WithTracing(provider =>
        provider
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddJaegerExporter(options =>
            {
                var config = builder.Configuration.GetSection(JaegerOptions.Prefix).Get<JaegerOptions>();

                if (config is null) return;

                options.AgentHost = config.AgentHost;
                options.AgentPort = config.AgentPort;
            }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseMiddleware<ExceptionMiddleware>();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

public partial class Program { }
