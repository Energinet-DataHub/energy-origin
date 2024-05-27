using AccessControl.API.Configuration;
using AccessControl.API.Swagger;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddControllersWithEnumsAsStrings();

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Audience = "f00b9b4d-3c59-4c40-b209-2ef87e509f54";
        options.Authority = "demo.energioprindelse.dk";
        options.MetadataAddress =
            "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0/.well-known/openid-configuration";
    });

builder.AddSerilog();
builder.Services.AddOpenTelemetryMetricsAndTracing("AccessControl.API", otlpOptions.ReceiverEndpoint);
builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var tokenValidationOptions =
    builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix)
    .ValidateDataAnnotations().ValidateOnStart();
var b2COptions = builder.Configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;
builder.Services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddB2CAndTokenValidation(b2COptions, tokenValidationOptions);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddVersioningToApi();
builder.Services.AddSwagger("access-control");
builder.Services.AddSwaggerGen(c =>
    c.DocumentFilter<ProblemDetailsDocumentFilter>());
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.AddSwagger("access-control");

app.MapControllers();

app.Run();

public partial class Program { }
