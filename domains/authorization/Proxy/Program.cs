using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Options;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Proxy.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
            new HeaderApiVersionReader("EO_API_VERSION"),
            new UrlSegmentApiVersionReader()
        );
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "VVV";
        options.SubstituteApiVersionInUrl = true;
    });

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
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

builder.Services.AttachOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var tokenConfiguration = builder.Configuration.GetSection(TokenValidationOptions.Prefix);
var tokenOptions = tokenConfiguration.Get<TokenValidationOptions>()!;

builder.Services.AttachOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var b2cConfiguration = builder.Configuration.GetSection(B2COptions.Prefix);
var b2cOptions = b2cConfiguration.Get<B2COptions>()!;

builder.Services.AddB2CAndTokenValidation(b2cOptions, tokenOptions);

builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

//app.UsePathBase("/wallet-api");

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

app.Run();


public partial class Program
{

}
