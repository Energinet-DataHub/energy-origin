using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.OpenTelemetry;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using OpenTelemetry;
using Proxy;
using Proxy.Options;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwagger("Wallet API");

builder.Services.AddSwaggerGen(c =>
{
    c.IgnoreObsoleteActions();
    c.DocumentFilter<WalletTagDocumentFilter>();
});

builder.Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new HeaderApiVersionReader("X-API-Version"),
            new UrlSegmentApiVersionReader()
        );
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.AddSerilogWithoutOutboxLogs();

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.Services.AddOpenTelemetryMetricsAndTracing("Proxy", otlpOptions.ReceiverEndpoint);

var proxyOptions = builder.Configuration.GetSection(ProxyOptions.Prefix).Get<ProxyOptions>()!;
builder.Services.AddOptions<ProxyOptions>()
    .BindConfiguration(ProxyOptions.Prefix)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient("Proxy", options => options.BaseAddress = new Uri(
    proxyOptions.WalletBaseUrl.AbsoluteUri,
    UriKind.Absolute));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AttachOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var b2cConfiguration = builder.Configuration.GetSection(B2COptions.Prefix);
var b2cOptions = b2cConfiguration.Get<B2COptions>()!;

builder.Services.AddB2C(b2cOptions);

builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.AddSwagger(app.Environment, "wallet-api");

app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapSwagger();

if (args.Contains("--swagger"))
{
    app.BuildSwaggerYamlFile(builder.Environment, "proxy.yaml");
}
else
{
    app.Run();
}

public partial class Program
{
}
