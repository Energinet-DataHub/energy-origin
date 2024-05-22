using System.Text.Json;
using System.Text.Json.Serialization;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Options;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Proxy.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwagger("ProjectOrigin.WalletSystem.Server");

builder.Services.AddSwaggerGen(c =>
{
    c.DocumentFilter<WalletTagDocumentFilter>();
});

builder.Services.AddVersioningToApi();

builder.Services.AddHttpClient("Proxy", options =>
{
    options.BaseAddress = new Uri("http://localhost:5182/", UriKind.Absolute); //new Uri(builder.Configuration["WalletServiceUrl"]!); // Maybe validate WalletServiceUrl, and throw startup exception.
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        //o.JsonSerializerOptions.Converters.Add(new IHDPublicKeyConverter(algorithm)); TODO: Find out if we need this
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

public class WalletTagDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Check if the "Contracts" tag already exists to avoid duplicates
        if (!swaggerDoc.Tags.Any(tag => tag.Name == "Wallet"))
        {
            swaggerDoc.Tags.Add(new OpenApiTag
            {
                Name = "Wallet",
                Description = "The Wallet is essential for Energy Origin," +
                              " since it keeps track of all the user’s Granular Certificates" +
                              " – both the ones generated from the user’s own metering points," +
                              " but also the ones transferred from other users." +
                              " In other words, the Wallet will hold all available certificates for the user." +
                              " Moreover, it will show all transfers, that may have been made," +
                              " to other users’ wallets as well.\n"
            });
        }
    }
}

