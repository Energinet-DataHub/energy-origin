using API.Middleware;
using API.Options;
using API.Repositories;
using API.Repositories.Data;
using API.Repositories.Data.Interfaces;
using API.Repositories.Interfaces;
using API.Services;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using IdentityModel.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var log = new LoggerConfiguration()
    .Filter.ByExcluding("RequestPath like '/health%'")
    .Filter.ByExcluding("RequestPath like '/metrics%'")
    .Enrich.WithSpan();

var console = builder.Environment.IsDevelopment()
    ? log.WriteTo.Console()
    : log.WriteTo.Console(new JsonFormatter());

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(console.CreateLogger());

var tokenConfiguration = builder.Configuration.GetSection(TokenOptions.Prefix);
var tokenOptions = tokenConfiguration.Get<TokenOptions>()!;

var databaseConfiguration = builder.Configuration.GetSection(DatabaseOptions.Prefix);
var databaseOptions = databaseConfiguration.Get<DatabaseOptions>()!;

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddAuthorization(options =>
{
    var organizationOwnerPolicy = builder.Services.BuildServiceProvider().GetRequiredService<OrganizationOwnerPolicy>();
    options.AddPolicy(nameof(OrganizationOwnerPolicy), policy => policy.Requirements.Add(organizationOwnerPolicy));
});

builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<CryptographyOptions>().BindConfiguration(CryptographyOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<TermsOptions>().BindConfiguration(TermsOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<TokenOptions>().BindConfiguration(TokenOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<OidcOptions>().BindConfiguration(OidcOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<IdentityProviderOptions>().BindConfiguration(IdentityProviderOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

if (builder.Environment.IsDevelopment() == false)
{
    builder.Services.AddOptions<DataSyncOptions>().BindConfiguration(DataSyncOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
}

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
builder.Services.AddSingleton<IMetrics, Metrics>();
builder.Services.AddScoped<OrganizationOwnerPolicy>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserDataContext, DataContext>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICompanyDataContext, DataContext>();
builder.Services.AddScoped<IUserProviderService, UserProviderService>();
builder.Services.AddScoped<IUserProviderRepository, UserProviderRepository>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserProviderDataContext, DataContext>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(provider =>
        provider
            .AddMeter(Metrics.Name)
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Metrics.Name))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(o =>
            {
                o.Endpoint = otlpOptions.ReceiverEndpoint;
            }))
    .WithTracing(provider =>
        provider
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Metrics.Name))
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(o =>
            {
                o.Endpoint = otlpOptions.ReceiverEndpoint;
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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

try
{
    app.Run();
}
catch (Exception e)
{
    app.Logger.LogError(e, "An exception has occurred while starting up.");
    throw;
}

public partial class Program { }
