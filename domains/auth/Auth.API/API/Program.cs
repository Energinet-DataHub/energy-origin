using System.Security.Cryptography;
using System.Text;
using API.Options;
using API.Utilities;
using IdentityModel.Client;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Json;

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(Encoding.UTF8.GetString(tokenOptions.PublicKeyPem));

    options.TokenValidationParameters = new()
    {
        IssuerSigningKey = new RsaSecurityKey(rsa),
        ValidAudience = tokenOptions.Audience,
        ValidIssuer = tokenOptions.Issuer,
    };
});

builder.Services.AddSingleton<IDiscoveryCache>(providers =>
{
    var options = providers.GetRequiredService<IOptions<OidcOptions>>();
    return new DiscoveryCache(options.Value.AuthorityUri.AbsoluteUri)
    {
        CacheDuration = options.Value.CacheDuration
    };
});

builder.Services.AddSingleton<ICryptography>(providers =>
{
    return new Cryptography("secretsecretsecretsecret");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
