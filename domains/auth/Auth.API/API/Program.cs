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

builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.Prefix));
builder.Services.Configure<TermsOptions>(builder.Configuration.GetSection(TermsOptions.Prefix));
builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection(TokenOptions.Prefix));

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

builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

builder.Services.AddSingleton<IDiscoveryCache>(providers =>
{
    var options = providers.GetRequiredService<IOptions<OidcOptions>>();
    return new DiscoveryCache(options.Value.AuthorityUri.AbsoluteUri)
    {
        CacheDuration = options.Value.CacheDuration
    };
});
builder.Services.AddSingleton<ICryptography>(providers => new Cryptography("secretsecretsecretsecret"));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserDataContext, DataContext>();
builder.Services.AddScoped<ITokenIssuer>(providers =>
{
    var termsOptions = providers.GetRequiredService<IOptions<TermsOptions>>().Value;
    var tokenOptions = providers.GetRequiredService<IOptions<TokenOptions>>().Value;
    var cryptography = providers.GetRequiredService<ICryptography>();
    var userService = providers.GetRequiredService<IUserService>();
    return new TokenIssuer(termsOptions, tokenOptions, cryptography, userService);
});
builder.Services.AddScoped(providers =>
{
    var accessor = providers.GetRequiredService<IHttpContextAccessor>();
    var cryptography = providers.GetRequiredService<ICryptography>();
    return new UserDescriptor(cryptography, accessor?.HttpContext?.User);
});

var app = builder.Build();

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
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
