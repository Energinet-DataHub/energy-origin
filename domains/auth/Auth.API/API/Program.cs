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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
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

if (builder.Environment.IsTest())
{
    builder.Logging.ClearProviders();
}

var tokenConfiguration = builder.Configuration.GetSection(TokenOptions.Prefix);
var tokenOptions = tokenConfiguration.Get<TokenOptions>()!;

var databaseConfiguration = builder.Configuration.GetSection(DatabaseOptions.Prefix);
var databaseOptions = databaseConfiguration.Get<DatabaseOptions>()!;

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddFeatureManagement();

builder.Services.AttachOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AttachOptions<CryptographyOptions>().BindConfiguration(CryptographyOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AttachOptions<TermsOptions>().BindConfiguration(TermsOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AttachOptions<TokenOptions>().BindConfiguration(TokenOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AttachOptions<OidcOptions>().BindConfiguration(OidcOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AttachOptions<IdentityProviderOptions>().BindConfiguration(IdentityProviderOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AttachOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AttachOptions<RoleOptions>().BindConfiguration(RoleOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AttachOptions<DataHubFacadeOptions>().BindConfiguration(DataHubFacadeOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

builder.Services.AddGrpcClient<Relation.V1.Relation.RelationClient>((sp, o) =>
{
    var options = sp.GetRequiredService<IOptions<DataHubFacadeOptions>>().Value;
    o.Address = new Uri(options.Url);
});

builder.AddTokenValidation(new TokenValidationOptions
{
    PublicKey = tokenOptions.PublicKeyPem,
    Audience = tokenOptions.Audience,
    Issuer = tokenOptions.Issuer
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

var dataSource = DataContext.GenerateNpgsqlDataSource(databaseOptions.ConnectionString);

builder.Services.AddSingleton(dataSource);
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(dataSource)
        .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

builder.Services.AddHealthChecks().AddNpgSql(sp => sp.GetRequiredService<NpgsqlDataSource>().ConnectionString);

builder.Services.AddSingleton<IDiscoveryCache>(providers =>
{
    var options = providers.GetRequiredService<OidcOptions>();
    return new DiscoveryCache(options.AuthorityUri.AbsoluteUri)
    {
        CacheDuration = options.CacheDuration
    };
});
builder.Services.AddSingleton<ICryptography, Cryptography>();
builder.Services.AddSingleton<ITokenIssuer, TokenIssuer>();
builder.Services.AddSingleton<IMetrics, Metrics>();

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
            .AddMeter(Metrics.Name)
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Metrics.Name))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint))
    .WithTracing(provider =>
        provider
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Metrics.Name))
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddGrpcClientInstrumentation(grpcOptions =>
            {
                grpcOptions.SuppressDownstreamInstrumentation = true;
                grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                    activity.SetTag("requestVersion", httpRequestMessage.Version);
                grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                    activity.SetTag("responseVersion", httpResponseMessage.Version);
            })
            .AddNpgsql()
            .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
    app.UseMiddleware<ExceptionMiddleware>();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

try
{
    app.Run();
}
catch (Exception e)
{
    app.Logger.LogError(e, "An exception has occurred while starting up");
    throw;
}

public partial class Program { }
