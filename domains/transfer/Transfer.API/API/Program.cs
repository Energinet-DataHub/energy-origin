using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using API.Cvr;
using API.Shared.Options;
using API.Transfer;
using API.Transfer.Api.Clients;
using API.Transfer.Api.Controllers;
using API.Transfer.Api.Exceptions;
using API.UnitOfWork;
using DataContext;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilog();

builder.Services.AddHttpClient<IAuthorizationClient, AuthorizationClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Authorization:BaseUrl"]!); // Do we want to fail in other way than nullpointer?
})
.AddPolicyHandler(RetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Should we start using real retry policies for HttpClient?
AsyncRetryPolicy<HttpResponseMessage> RetryPolicy()
{
    return HttpPolicyExtensions.HandleTransientHttpError()
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

// Should we start using circuit breaker for http client?
IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}

builder.Services.AddOpenTelemetryMetricsAndTracing("Transfer.API", otlpOptions.ReceiverEndpoint);

builder.Services.Configure<JsonOptions>(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddDbContext<DbContext, ApplicationDbContext>(
    (sp, options) =>
    {
        options.UseNpgsql(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString(),
            providerOptions => providerOptions.EnableRetryOnFailure()
        );
    },
    optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<ApplicationDbContext>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString());

builder.Services.AddValidatorsFromAssembly(typeof(API.Program).Assembly);

builder.Services.AddActivityLog(options => options.ServiceName = "transfer");


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddLogging();
builder.Services.AddTransfer();
builder.Services.AddCvr();
builder.Services.AddVersioningToApi();

builder.Services.AddSwagger("transfer");
builder.Services.AddSwaggerGen();
builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

var b2COptions = builder.Configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;
builder.Services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddB2C(b2COptions);

var app = builder.Build();

app.MapHealthChecks("/health");

app.AddSwagger("transfer");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseMiddleware<ExceptionHandlerMiddleware>();

var activityLogApiVersionSet = app.NewApiVersionSet("activitylog").Build();
app.UseActivityLogWithB2CSupport().WithApiVersionSet(activityLogApiVersionSet)
    .HasApiVersion(ApiVersions.Version1AsInt)
    .HasApiVersion(ApiVersions.Version20240515AsInt);

app.Run();

namespace API
{
    public partial class Program
    {
    }
}
