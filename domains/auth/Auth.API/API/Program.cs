using API.Configuration;
using API.Services;
using FluentValidation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Serilog;
using Serilog.Formatting.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

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

builder.Services.AddValidatorsFromAssemblyContaining<CookieOptions>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Inform Swagger about FluentValidation rules. See https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation for more details
builder.Services.AddTransient<IValidatorFactory, ServiceProviderValidatorFactory>();
builder.Services.AddFluentValidationRulesToSwagger();

builder.Services.AddHttpClient();

builder.Services.Configure<AuthOptions>(builder.Configuration);

builder.Services.AddScoped<ICryptographyService, CryptographyService>();
builder.Services.AddScoped<IOidcProviders, SignaturGruppen>();
builder.Services.AddScoped<IOidcService, OidcService>();
builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddSingleton<ITokenStorage, TokenStorage>();

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
