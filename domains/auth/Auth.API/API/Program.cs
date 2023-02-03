using API.Options;
using IdentityModel.Client;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Formatting.Json;

var logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection(TokenOptions.Prefix));
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.Prefix));
builder.Services.AddSingleton<IDiscoveryCache>(providers =>
{
    var options = providers.GetRequiredService<IOptions<OidcOptions>>();
    return new DiscoveryCache(options.Value.AuthorityUri.AbsoluteUri)
    {
        CacheDuration = options.Value.CacheDuration
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
