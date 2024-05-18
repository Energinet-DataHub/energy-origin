using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;

namespace Proxy;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Load token validation options from configuration
        var tokenValidationOptions = builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
        builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
        builder.AddTokenValidation(tokenValidationOptions);

        // Use the private key from the configuration
        var privateKeyBase64 = builder.Configuration["TokenValidation:PrivateKey"];
        var privateKey = Convert.FromBase64String(privateKeyBase64!);
        var tokenSigner = new TokenSigner(privateKey);
        builder.Services.AddSingleton(tokenSigner);
        builder.Services.AddOcelot().AddKubernetes();

        var app = builder.Build();

        app.UseMiddleware<TokenModificationMiddleware>();

        app.UseOcelot().Wait();

        app.Run();
    }
}
