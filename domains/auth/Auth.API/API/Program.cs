using System.Security.Cryptography;
using System.Text;
using API.Middleware;
using API.Options;
using API.Repositories;
using API.Repositories.Data;
using API.Services;
using API.Utilities;
using IdentityModel.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;

var logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

var tokenConfiguration = builder.Configuration.GetSection(TokenOptions.Prefix);
var tokenOptions = tokenConfiguration.Get<TokenOptions>()!;

builder.Services.Configure<TokenOptions>(tokenConfiguration);
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

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(Encoding.UTF8.GetString(tokenOptions.PublicKeyPem));

    options.MapInboundClaims = false;

    options.TokenValidationParameters = new()
    {
        IssuerSigningKey = new RsaSecurityKey(rsa),
        ValidAudience = tokenOptions.Audience,
        ValidIssuer = tokenOptions.Issuer,
    };
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

builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

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

var serviceName = "Auth.API";
var serviceVersion = "1.0.0";

var appResourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion);

//builder.Services.AddOpenTelemetryMetrics(metricProviderBuilder =>
//{
//    metricProviderBuilder
//        .AddHttpClientInstrumentation()
//        .AddAspNetCoreInstrumentation()
//        .AddMeter("MyApplicationMetrics")
//        .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
//});

builder.Services.AddOpenTelemetry().WithMetrics(metricProviderBuilder =>
{
    metricProviderBuilder
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddMeter("MyApplicationMetrics")
        .AddPrometheusExporter(options =>
        {
            options.ScrapeResponseCacheDurationMilliseconds = 0;
        });
});

//builder.Services.AddOpenTelemetry()
//    //.WithTracing(tracerProviderBuilder =>
//    //{
//    //    tracerProviderBuilder
//    //        .AddConsoleExporter()
//    //        .AddSource(serviceName)
//    //        .SetResourceBuilder(appResourceBuilder)
//    //        .AddHttpClientInstrumentation()
//    //        .AddAspNetCoreInstrumentation();



//    //    //tracerProviderBuilder.AddJaegerExporter();
//    //})
//    .WithMetrics(metricProviderBuilder =>
//    {
//        metricProviderBuilder
//                                  //.AddConsoleExporter()
//                                  //.AddPrometheusExporter()
//            .AddHttpClientInstrumentation()
//            .AddAspNetCoreInstrumentation()
//            .AddMeter("MyApplicationMetrics")
//            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));

//            //.SetResourceBuilder(appResourceBuilder)


//        //metricProviderBuilder.AddPrometheusExporter();
//    });

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseMiddleware<ExceptionMiddleware>();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

public partial class Program { }
