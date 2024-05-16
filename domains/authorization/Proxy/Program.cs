using System.ComponentModel.DataAnnotations;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.Extensions.Options;
using Proxy;
using Proxy.Services;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwagger("authorization-proxy");
builder.Services.AddSwaggerGen();

builder.Services.AddVersioningToApi();

builder.Services.AddHttpClient<IWalletProxyService, WalletProxyService>(options =>
{
    options.BaseAddress =
        new Uri("https://google.com"); //new Uri(builder.Configuration["WalletServiceUrl"]!); // Maybe validate WalletServiceUrl, and throw startup exception.
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



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}

app.AddSwagger(app.Environment, "authorization-proxy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


public static class ServiceCollectionExtensions
{
    public static OptionsBuilder<T> AttachOptions<T>(this IServiceCollection services) where T : class
    {
        var builder = services.AddOptions<T>();
        services.AddTransient(x => x.GetRequiredService<IOptions<T>>().Value);
        return builder;
    }
}
