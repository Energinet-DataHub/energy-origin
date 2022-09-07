using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Configuration;
using API.Helpers;
using API.Models;
using API.Repository;
using API.Services;
using API.Services.OidcProviders;
using API.TokenStorage;
using FluentValidation;
using Serilog;
using Serilog.Formatting.Json;

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

builder.Services.Configure<AuthOptions>(builder.Configuration);
builder.Services.AddScoped<ICryptography, Cryptography>();
builder.Services.AddScoped<IOidcService, SignaturGruppen>();
builder.Services.AddScoped<IValidator<AuthState>, InvalidateAuthStateValidator>();
builder.Services.AddScoped<ICookies, Cookies>();
builder.Services.AddScoped<ITokenStorage, TokenStorage>();
builder.Services.AddScoped<IPrivacyPolicyStorage, PrivacyPolicyStorage>();

var app = builder.Build();

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
