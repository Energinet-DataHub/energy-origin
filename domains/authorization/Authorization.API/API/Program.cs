using API.Authorization;
using API.Configuration;
using API.RabbitMq;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilog();

builder.Services.AddOpenTelemetryMetricsAndTracing("Authorization.API", otlpOptions.ReceiverEndpoint);

builder.Services.AddControllersWithEnumsAsStrings();

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Audience = "f00b9b4d-3c59-4c40-b209-2ef87e509f54";
        options.MetadataAddress = "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0/.well-known/openid-configuration";
    });
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// builder.Services.AddDbContext<DbContext, ApplicationDbContext>(
//     options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")),
//     optionsLifetime: ServiceLifetime.Singleton);
// builder.Services.AddDbContextFactory<ApplicationDbContext>();
// builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//builder.Services.AddHealthChecks().AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);

//builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddAuthorizationApi();
//builder.Services.AddVersioningToApi();

var app = builder.Build();

//app.MapHealthChecks("/health");

//app.AddSwagger("authorization");

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace API
{
    public partial class Program
    {
    }
}
