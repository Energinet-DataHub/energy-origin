using API.Middleware;
using API.Options;
using API.Repositories;
using API.Repositories.Data;
using API.Repositories.Data.Interfaces;
using API.Repositories.Interfaces;
using API.Services;
using API.Services.Interfaces;
using API.Utilities;
using EnergyOrigin.TokenValidation.Utilities;
using API.Utilities.Interfaces;
using IdentityModel.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Json;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Options;

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
builder.Services.Configure<CryptographyOptions>(builder.Configuration.GetSection(CryptographyOptions.Prefix));
builder.Services.Configure<TermsOptions>(builder.Configuration.GetSection(TermsOptions.Prefix));
builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection(TokenOptions.Prefix));
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.Prefix));
builder.Services.Configure<DataSyncOptions>(builder.Configuration.GetSection(DataSyncOptions.Prefix));

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
builder.Services.Configure<IdentityProviderOptions>(builder.Configuration.GetSection(IdentityProviderOptions.Prefix));

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

builder.Services.AddSingleton(new NpgsqlDataSourceBuilder($"Host={databaseOptions.Host}; Port={databaseOptions.Port}; Database={databaseOptions.Name}; Username={databaseOptions.User}; Password={databaseOptions.Password};"));
builder.Services.AddDbContext<DataContext>((serviceProvider, options) => options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSourceBuilder>().Build()));

builder.Services.AddSingleton<IDiscoveryCache>(providers =>
{
    var options = providers.GetRequiredService<IOptions<OidcOptions>>();
    return new DiscoveryCache(options.Value.AuthorityUri.AbsoluteUri)
    {
        CacheDuration = options.Value.CacheDuration
    };
});
builder.Services.AddSingleton<ICryptography, Cryptography>();
builder.Services.AddSingleton<IUserDescriptorMapper, UserDescriptorMapper>();
builder.Services.AddSingleton<ITokenIssuer, TokenIssuer>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserDataContext, DataContext>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICompanyDataContext, DataContext>();
builder.Services.AddScoped<IUserProviderService, UserProviderService>();
builder.Services.AddScoped<IUserProviderRepository, UserProviderRepository>();
builder.Services.AddScoped<IUserProviderDataContext, DataContext>();

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
else if (!app.Environment.IsTest())
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
